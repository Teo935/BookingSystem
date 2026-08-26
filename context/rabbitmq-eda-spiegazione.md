# RabbitMQ ed Event-Driven Architecture — spiegazione

> Scritto il 2026-08-24 su richiesta, per poter rileggere senza dover
> richiedere di nuovo la spiegazione. Riferimento: sessione del 2026-08-24
> "RabbitMQ ed Event-Driven Architecture (EDA) su creazione/cancellazione
> prenotazioni" in `context/sessions.md`.

## 1. Il problema che si vuole risolvere

Immagina `POST /api/bookings`: oggi il flusso è "valida i dati → salva la
prenotazione su SQL Server → rispondi al client". Se in futuro vuoi anche
mandare un'email di conferma, la soluzione più ovvia sarebbe chiamare
direttamente un `IEmailService` dentro `BookingService`, in modo sincrono:

```
Controller → BookingService.CreateBookingAsync()
                ↓
         salva su SQL Server
                ↓
         chiama IEmailService.SendAsync(...)   ← qui il problema
                ↓
         ritorna la risposta HTTP
```

Questo approccio ha due difetti concreti:

1. **Il client aspetta anche l'email.** Se il servizio email è lento (anche
   solo 2-3 secondi), la richiesta HTTP di chi prenota resta appesa per
   quel tempo, anche se la prenotazione è già stata salvata con successo.
2. **Se l'invio email fallisce, cosa succede alla prenotazione?** Se lanci
   un'eccezione, rischi di far fallire un'operazione (la prenotazione) che
   in realtà era già andata a buon fine, solo perché un servizio secondario
   (l'email) non ha funzionato. Se invece ignori l'errore in silenzio,
   perdi ogni traccia che l'invio non è mai partito.

**Event-Driven Architecture (EDA)**, in italiano "architettura guidata dagli
eventi", risolve il problema separando le due cose nel tempo: invece di
*chiamare direttamente* il servizio che deve reagire, `BookingService`
**pubblica un messaggio** che dice "è successo questo" (l'evento, es.
"prenotazione creata") e non si preoccupa più di chi lo leggerà né di
quando. Chi è interessato (in questo progetto, il servizio di notifica) lo
legge quando può, in un processo separato, senza bloccare la richiesta
HTTP originale.

```
Controller → BookingService.CreateBookingAsync()
                ↓
         salva su SQL Server
                ↓
         pubblica un evento "BookingCreated" (operazione rapidissima:
         mette un messaggio in una coda, non aspetta che qualcuno lo legga)
                ↓
         ritorna subito la risposta HTTP

                                    (in un momento successivo, indipendente)
                              Consumer → legge l'evento → invia l'email
```

## 2. RabbitMQ — cos'è un "message broker"

RabbitMQ è un **message broker**: un servizio (qui gira in un container
Docker separato, come già fanno SQL Server e Redis) il cui unico compito è
ricevere messaggi da chi li produce e consegnarli a chi li consuma, tenendo
tra i due un'area di attesa (una **coda**) se il consumatore non è pronto
subito.

I concetti chiave, con la terminologia usata in questo progetto:

- **Producer** (produttore): chi pubblica un messaggio. Nel nostro caso,
  `BookingService`.
- **Consumer** (consumatore): chi legge ed elabora un messaggio. Nel nostro
  caso, `BookingNotificationConsumer`.
- **Queue** (coda): una lista FIFO (first-in-first-out, il primo messaggio
  che entra è il primo che esce) dove i messaggi restano finché un consumer
  non li preleva. Sopravvive anche se, per qualche secondo, nessun consumer
  è collegato.
- **Exchange**: il producer non scrive *mai* direttamente in una coda —
  scrive in un exchange, che è il "postino" che decide su quale coda (o
  quali code) instradare il messaggio. Esistono diversi tipi di exchange;
  quello usato qui è di tipo **topic**, che instrada in base a una
  "etichetta" testuale (la routing key) che può anche contenere pattern
  con wildcard (qui non li usiamo, ma è il motivo per cui questo tipo di
  exchange è comune: permette di aggiungere filtri più sofisticati in
  futuro senza cambiare architettura).
- **Routing key**: l'etichetta che il producer allega al messaggio quando
  lo pubblica sull'exchange (qui: `booking.created` o `booking.cancelled`).
- **Binding**: il collegamento, dichiarato in anticipo, tra un exchange e
  una coda, con l'indicazione di quale routing key deve finire in quella
  coda. Senza binding, l'exchange riceve il messaggio ma non sa dove
  instradarlo e lo scarta.
- **Ack / Nack** (acknowledgement / negative acknowledgement): quando un
  consumer riceve un messaggio, RabbitMQ lo considera ancora "in transito"
  finché il consumer non conferma esplicitamente di averlo elaborato con
  successo (**ack**). Se il consumer si disconnette o crasha prima di
  confermare, RabbitMQ lo rimette in coda per un altro tentativo. Se il
  consumer invece capisce che il messaggio è "rotto" (es. non riesce a
  interpretarlo) può scartarlo esplicitamente con un **nack**, dicendo a
  RabbitMQ di non ritentare.
- **Durable**: una coda o un exchange dichiarati "durable" sopravvivono a
  un riavvio del broker (altrimenti, per default, esistono solo finché
  RabbitMQ resta acceso).

## 3. Perché proprio qui — l'analisi fatta prima di scrivere codice

Prima di implementare qualunque cosa, ho analizzato il progetto per capire
*dove* un evento avesse davvero senso, invece di aggiungerlo ovunque:

- `BookingService.CreateBookingAsync` e `CancelBookingAsync` sono gli unici
  due punti dove succede qualcosa di realmente "interessante" per un
  osservatore esterno, con un motivo concreto per disaccoppiare l'azione
  conseguente (una notifica) dalla richiesta HTTP.
- Ho scartato un ipotetico evento `RoomDeleted`: `RoomService.DeleteRoomAsync`
  blocca già la cancellazione se la stanza ha prenotazioni attive
  (`RoomDeleteResult.Conflict`), quindi non esiste mai un caso in cui una
  stanza venga cancellata *con* prenotazioni da notificare.
- Ho scartato un ipotetico evento `UserRegistered`: nel progetto non esiste
  alcuna infrastruttura di invio email (nessun `IEmailSender`/`SmtpClient`),
  quindi sarebbe stato un evento "orfano", senza nessuno pronto ad
  ascoltarlo.

## 4. La topologia scelta

```
                    exchange "booking.events" (tipo: topic)
                    /                              \
      routing key "booking.created"      routing key "booking.cancelled"
                    \                              /
                     coda "booking.notifications"
                                |
                    BookingNotificationConsumer
                     (1 solo consumer, in ascolto)
```

Un solo exchange, due routing key, una sola coda con due binding — la
versione più semplice che comunque dimostra i concetti reali di RabbitMQ
(exchange, routing key, binding), invece di banalizzare tutto con una coda
unica senza exchange dedicato.

## 5. I file nuovi, uno per uno

### 5.1 `BookingSystem.Application/Events/BookingCreatedEvent.cs` e `BookingCancelledEvent.cs`

Due `record` (un tipo C# pensato per dati immutabili, confrontabili per
valore) che descrivono "cosa è successo":

```csharp
public record BookingCreatedEvent(
    int BookingId, int RoomId, string RoomName, string? UserId,
    string GuestName, DateTime CheckIn, DateTime CheckOut, DateTime OccurredAtUtc);
```

Vivono nel progetto **Application**, non in Infrastructure: sono "contratti
di dominio", non hanno alcun riferimento a RabbitMQ. `BookingCancelledEvent`
non include `RoomName` perché `CancelBookingAsync` carica la prenotazione
con `GetByIdAsync` (che non include i dati della stanza collegata), a
differenza di `GetBookingAsync` che usa `GetByIdWithRoomAsync` — aggiungere
quel dato avrebbe richiesto cambiare quella query per un beneficio marginale.

### 5.2 `BookingSystem.Application/Interfaces/IEventPublisher.cs`

```csharp
public interface IEventPublisher
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class;
}
```

Questo è il "contratto astratto": dice solo "so pubblicare un evento",
senza sapere se dietro c'è RabbitMQ, un altro broker, o altro ancora —
stesso principio già usato da `ICacheService` per Redis (vedi
[redis-caching-spiegazione.md](redis-caching-spiegazione.md)) o da
`IBookingRepository` per il database: il livello Application dipende solo
da astrazioni, mai da dettagli tecnici concreti (è l'ultima delle 5 regole
SOLID, il **Dependency Inversion Principle**).

### 5.3 `BookingSystem.Infrastructure/Messaging/RabbitMqSettings.cs`

```csharp
public class RabbitMqSettings
{
    public string ExchangeName { get; set; } = "booking.events";
    public string NotificationsQueueName { get; set; } = "booking.notifications";
}
```

Una classe di configurazione (POCO Options), stesso stile già usato da
`CacheSettings`/`JwtSettings`: i nomi di exchange e coda non sono
hardcoded nel codice, arrivano da `appsettings.json`.

### 5.4 `BookingSystem.Infrastructure/Messaging/RabbitMqEventPublisher.cs`

Implementa `IEventPublisher` usando la libreria `RabbitMQ.Client`. Il
punto più importante di questa classe:

```csharp
public async Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default) where TEvent : class
{
    try
    {
        var routingKey = GetRoutingKey<TEvent>();
        await using var channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);
        await channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event);
        var properties = new BasicProperties
        {
            ContentType = "application/json",
            Type = typeof(TEvent).Name,
            DeliveryMode = DeliveryModes.Persistent
        };

        await channel.BasicPublishAsync(_settings.ExchangeName, routingKey, mandatory: false, properties, body, cancellationToken);
    }
    catch (Exception ex)
    {
        _logger.LogWarning(ex, "Failed to publish event {EventType} to RabbitMQ.", typeof(TEvent).Name);
    }
}
```

**Tutto il metodo è dentro un `try/catch` che non rilancia mai
l'eccezione.** Questa è la decisione di design più importante di tutta la
funzionalità: se RabbitMQ non è raggiungibile (spento, in manutenzione,
problemi di rete), l'unica conseguenza è un log di avviso — la
prenotazione resta comunque creata/cancellata con successo su SQL Server,
e il client riceve comunque una risposta `200`. Se invece avessi lasciato
propagare l'eccezione, un problema del servizio di notifica (secondario)
avrebbe potuto far fallire un'operazione di business (primaria) che in
realtà era già riuscita.

Un `channel` (canale) è un "tubo" leggero dentro una connessione TCP
condivisa — RabbitMQ.Client raccomanda di aprire una connessione sola (qui
registrata come singleton, vedi punto 5.7) e un canale per ogni operazione
o gruppo di operazioni, invece di aprire connessioni multiple.

`GetRoutingKey<TEvent>()` è una semplice mappatura esplicita (uno `switch`
su `typeof(TEvent).Name`) tra il tipo C# dell'evento e la routing key
testuale da usare — niente riflessione "magica", solo un elenco leggibile.

### 5.5 `BookingSystem.Infrastructure/Messaging/BookingNotificationHandler.cs`

La logica vera e propria del consumer, isolata dal "tubo" RabbitMQ per
poter essere testata senza bisogno di un broker reale:

```csharp
public Task HandleBookingCreatedAsync(BookingCreatedEvent evt)
{
    _logger.LogInformation(
        "[Notifica] Simulazione invio email di conferma a {GuestName} per la prenotazione #{BookingId} ({RoomName}, {CheckIn:d} - {CheckOut:d}).",
        evt.GuestName, evt.BookingId, evt.RoomName, evt.CheckIn, evt.CheckOut);
    return Task.CompletedTask;
}
```

In un progetto reale, qui ci sarebbe la chiamata a un vero servizio email
(es. SendGrid, SMTP); in questo progetto didattico, per restare semplici
senza aggiungere una dipendenza esterna in più, la "notifica" è simulata
con un log ben visibile — il punto pedagogico (dimostrare il disaccoppiamento
produttore/consumatore) resta identico.

### 5.6 `BookingSystem.Infrastructure/Messaging/BookingNotificationConsumer.cs`

Un `BackgroundService` — una classe base di ASP.NET Core pensata per far
girare codice in sottofondo per tutta la vita dell'applicazione, in
parallelo alla gestione delle richieste HTTP. Al suo avvio:

```csharp
protected override async Task ExecuteAsync(CancellationToken stoppingToken)
{
    _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

    await _channel.ExchangeDeclareAsync(_settings.ExchangeName, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);
    await _channel.QueueDeclareAsync(_settings.NotificationsQueueName, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
    await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.created", cancellationToken: stoppingToken);
    await _channel.QueueBindAsync(_settings.NotificationsQueueName, _settings.ExchangeName, "booking.cancelled", cancellationToken: stoppingToken);
    await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 1, global: false, cancellationToken: stoppingToken);

    var consumer = new AsyncEventingBasicConsumer(_channel);
    consumer.ReceivedAsync += OnMessageReceivedAsync;

    await _channel.BasicConsumeAsync(_settings.NotificationsQueueName, autoAck: false, consumer, cancellationToken: stoppingToken);
}
```

Passo per passo:

1. **Dichiara** l'exchange e la coda (operazione idempotente: se esistono
   già, RabbitMQ non fa nulla — questo evita di dover creare manualmente
   la topologia prima di avviare l'app).
2. **Collega** la coda all'exchange con i due binding (`booking.created`,
   `booking.cancelled`).
3. `BasicQosAsync(prefetchCount: 1, ...)`: dice a RabbitMQ "non mandarmi
   un nuovo messaggio finché non ho confermato (ack) il precedente" — così
   il consumer elabora un messaggio alla volta, in ordine, senza sovraccaricarsi.
4. Registra un `AsyncEventingBasicConsumer`: un oggetto che scatena
   `OnMessageReceivedAsync` ogni volta che arriva un nuovo messaggio.
5. `BasicConsumeAsync(..., autoAck: false, ...)`: **`autoAck: false`** è
   la scelta cruciale — dice a RabbitMQ di aspettare la conferma esplicita
   invece di considerare il messaggio "consegnato con successo" nell'istante
   stesso in cui lo consegna. Con `autoAck: true`, un crash del consumer a
   metà elaborazione perderebbe silenziosamente il messaggio.

Il gestore vero e proprio:

```csharp
private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
{
    try
    {
        var json = Encoding.UTF8.GetString(ea.Body.Span);
        switch (ea.BasicProperties.Type)
        {
            case nameof(BookingCreatedEvent):
                var created = JsonSerializer.Deserialize<BookingCreatedEvent>(json) ?? throw new JsonException(...);
                await _handler.HandleBookingCreatedAsync(created);
                break;
            case nameof(BookingCancelledEvent):
                // ... stesso schema
                break;
            default:
                _logger.LogWarning("Ricevuto messaggio con tipo sconosciuto '{Type}', scartato.", ea.BasicProperties.Type);
                break;
        }
        await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Errore durante l'elaborazione del messaggio di notifica prenotazione, messaggio scartato.");
        await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
    }
}
```

Legge il campo `Type` che il producer ha impostato sulle proprietà del
messaggio (vedi punto 5.4) per capire *quale* dei due eventi è arrivato
(la coda li riceve entrambi mescolati, dato che è la stessa per entrambe
le routing key), deserializza il JSON, delega a `BookingNotificationHandler`
e infine conferma (`BasicAckAsync`). Se qualcosa va storto (JSON corrotto,
tipo sconosciuto, eccezione nell'handler), scarta il messaggio con
`BasicNackAsync(..., requeue: false)` — **senza rimetterlo in coda**, per
evitare che un messaggio permanentemente "rotto" venga ritentato all'infinito
in un ciclo che non si risolverà mai da solo (un limite consapevole: in un
sistema di produzione, a questo punto si userebbe una "dead-letter queue",
una coda separata dove finiscono i messaggi scartati, per poterli
ispezionare più tardi invece di perderli — qui, per restare semplici, si è
scelto di accettare la perdita di un messaggio malformato).

### 5.7 `BookingService.cs` — dove viene pubblicato l'evento

Costruttore (righe 9-20): aggiunta la terza dipendenza `IEventPublisher`,
iniettata insieme alle due già esistenti (`IBookingRepository`,
`IRoomRepository`).

`CreateBookingAsync` (righe 51-57):

```csharp
var created = await _bookingRepository.AddAsync(booking);

await _eventPublisher.PublishAsync(new BookingCreatedEvent(
    created.Id, created.RoomId, room.Name, created.UserId, created.GuestName,
    created.CheckIn, created.CheckOut, DateTime.UtcNow));

return (true, null, created);
```

L'evento viene pubblicato **solo dopo** che `AddAsync` è già andato a buon
fine — se la validazione fallisce prima (date invalide, stanza inesistente,
sovrapposizione), il codice esce con un `return` anticipato e l'evento non
viene mai pubblicato. `room.Name` è disponibile perché la stanza è già
stata caricata qualche riga sopra per la validazione (riga 29).

`CancelBookingAsync` (righe 88-92): stesso schema, l'evento
`BookingCancelledEvent` viene pubblicato subito dopo `RemoveAsync`, usando i
dati di `booking` già caricati in memoria prima della cancellazione (la
riga `await _bookingRepository.RemoveAsync(booking)` non svuota l'oggetto
C#, solo la riga corrispondente nel database).

### 5.8 `Program.cs` — il collegamento (Dependency Injection)

Righe 96-106:

```csharp
builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
var rabbitMqConnectionString = builder.Configuration.GetConnectionString("RabbitMq")
    ?? throw new InvalidOperationException("Missing 'RabbitMq' connection string.");
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory { Uri = new Uri(rabbitMqConnectionString) };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddSingleton<BookingNotificationHandler>();
builder.Services.AddHostedService<BookingNotificationConsumer>();
```

Stesso pattern già visto per Redis: una connessione (qui `IConnection` di
RabbitMQ, lì `IConnectionMultiplexer` di Redis) registrata come `Singleton`
— cioè una sola istanza condivisa per tutta la vita dell'applicazione,
perché aprire una nuova connessione TCP per ogni richiesta sarebbe
inefficiente. `AddHostedService<BookingNotificationConsumer>()` è la
chiamata che dice ad ASP.NET Core "avvia questo `BackgroundService`
insieme all'applicazione e tienilo in esecuzione finché l'app non si
ferma" — nessuna modifica necessaria a `BookingsController`: l'unico
effetto visibile all'esterno del Service è la nuova dipendenza
`IEventPublisher` dentro `BookingService`.

## 6. Configurazione — `appsettings.json` e Docker

- `ConnectionStrings:RabbitMq` = `amqp://guest:guest@localhost:5672/` per
  sviluppo locale senza Docker (`guest/guest` sono le credenziali
  pubbliche di default di RabbitMQ, non un vero segreto).
- Sezione `RabbitMq` con `ExchangeName`/`NotificationsQueueName`.
- `docker-compose.yml`: nuovo servizio `rabbitmq` con l'immagine
  `rabbitmq:3-management` (include un'interfaccia web di amministrazione
  su `http://localhost:15672`, utile per "vedere" exchange, code e
  messaggi mentre studi), credenziali lette da `.env` tramite
  `RABBITMQ_USER`/`RABBITMQ_PASSWORD` (stesso schema già usato per
  `SA_PASSWORD` di SQL Server).

## 7. Resilienza — cosa succede se RabbitMQ si spegne

Ho verificato concretamente due scenari con `docker compose stop rabbitmq`:

1. **RabbitMQ giù, creo una prenotazione**: la richiesta HTTP torna
   comunque `200` con la prenotazione salvata regolarmente. Nei log
   compare solo: `Failed to publish event BookingCreatedEvent to RabbitMQ`
   (livello warning, non un errore che blocca nulla) — esattamente il
   comportamento voluto dal `try/catch` del punto 5.4.
2. **Riavvio RabbitMQ**: senza riavviare l'applicazione, sia il
   publisher sia il consumer sono tornati a funzionare da soli. Questo è
   merito di una funzionalità della libreria `RabbitMQ.Client` chiamata
   **automatic recovery** (attiva di default): la connessione (`IConnection`)
   registrata come singleton, sotto il cofano, rileva la disconnessione e
   ristabilisce da sola sia la connessione TCP sia i canali/binding già
   dichiarati, senza bisogno di codice scritto da noi per gestirlo.

## 8. Test scritti

- `BookingServiceTests.cs`: aggiunto un `Mock<IEventPublisher>` nel
  costruttore del test; ogni test verifica che `PublishAsync` venga
  chiamato **una volta** sui percorsi di successo (`ValidRequest`,
  `OwnerCancelsOwnBooking`, `AdminCancelsAnyBooking`) e **mai** sui
  percorsi di errore (date invalide, stanza inesistente, sovrapposizione,
  `NotFound`, `Forbidden`) — usando `Moq`, la stessa libreria di mock già
  in uso in tutto il progetto.
- `Messaging/BookingNotificationHandlerTests.cs`: testa
  `BookingNotificationHandler` da solo, con un `ILogger<T>` finto — nessuna
  dipendenza da RabbitMQ, perché quella classe non ne ha bisogno.
- **Nessun test dedicato** per `RabbitMqEventPublisher`/
  `BookingNotificationConsumer`: mockare `IConnection`/`IChannel` di una
  libreria di terze parti avrebbe un valore molto basso rispetto allo
  sforzo (staremmo testando che la libreria fa quello che dichiara di
  fare, non la nostra logica) — questi due componenti sono stati verificati
  con un vero broker acceso (vedi punto 9), che è il test più realistico
  possibile per codice che parla con un servizio esterno.

## 9. Verificato end-to-end

Con `docker compose up --build`:

- Management UI (`localhost:15672`): confermata la presenza dell'exchange
  `booking.events` (tipo topic) e della coda `booking.notifications` con
  **1 consumer attivo** subito dopo l'avvio dell'API.
- Creata una prenotazione via API → risposta HTTP immediata, poi nei log
  dell'api la riga `[Notifica] Simulazione invio email di conferma...`;
  contatori della coda (`publish`, `deliver`, `ack`) tutti a 1.
- Cancellata la prenotazione → log simmetrico di cancellazione.
- Test di resilienza (punto 7) confermato dal vivo.
- `dotnet test`: **57/57** superati (55 preesistenti + 2 nuovi).

## In sintesi

RabbitMQ qui fa da "buca delle lettere" tra `BookingService` (che scrive
"è successo questo" e se ne dimentica) e `BookingNotificationConsumer` (che
legge quando può). Nessuno dei due sa nulla dell'altro: `BookingService`
non sa nemmeno che esiste un consumer, e il consumer non sa nulla di come
sia stata creata la prenotazione. Questo disaccoppiamento è tutto il senso
dell'Event-Driven Architecture — permette di aggiungere in futuro un
secondo consumer (es. per aggiornare una dashboard, generare statistiche,
avvisare un sistema esterno) senza toccare `BookingService` di una virgola:
basterebbe collegare una nuova coda con un nuovo binding sullo stesso
exchange già esistente.
