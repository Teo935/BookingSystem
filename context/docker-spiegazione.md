# Docker e Docker Compose — spiegazione

> Scritto il 2026-08-24 su richiesta, per poter rileggere senza dover
> richiedere di nuovo la spiegazione. Riferimenti: sessione del 2026-07-28
> "Containerizzazione con Docker (Dockerfile + docker-compose)" e sessione
> del 2026-08-24 (aggiunta di RabbitMQ) in `context/sessions.md`.

## 1. Il problema che Docker risolve

Questo progetto, per funzionare, ha bisogno di quattro "cose" in esecuzione
contemporaneamente: l'API .NET, SQL Server, Redis e RabbitMQ. Installarle
tutte a mano sulla tua macchina (ognuna con la sua versione, la sua
configurazione, magari in conflitto con altri progetti che hai già
installato) è scomodo e difficile da riprodurre identico su un'altra
macchina (es. quella di un collega, o un server).

**Docker** risolve questo con l'idea di **container**: un ambiente isolato
e già pronto — con dentro tutto il necessario (sistema operativo minimale,
librerie, l'applicazione stessa) — che gira sopra la tua macchina senza
installare nulla di permanente sul sistema operativo host. Se cancelli il
container, non resta nulla (a meno che tu non abbia esplicitamente scelto
di salvare dei dati, vedi il punto 6 sui volumi). Se lo ricrei, riparte
identico a prima.

## 2. Concetti base

- **Image (immagine)**: un "modello" immutabile, tipo lo stampo di un
  biscotto — contiene tutto quello che serve per far girare qualcosa (es.
  `mcr.microsoft.com/mssql/server:2022-latest` è l'immagine ufficiale di
  SQL Server). Le immagini si scaricano da un **registry** (un archivio
  online di immagini pubbliche/private; il più comune è Docker Hub) oppure
  si costruiscono in locale a partire da un `Dockerfile` (punto 4).
- **Container**: un'istanza *in esecuzione* di un'immagine — il biscotto
  vero e proprio, ottenuto dallo stampo. Puoi avviare più container dalla
  stessa immagine, ognuno isolato dagli altri.
- **Dockerfile**: un file di testo con le istruzioni per **costruire**
  un'immagine personalizzata (nel nostro caso, l'immagine dell'API .NET,
  che non esiste già pronta su un registry perché è il nostro codice).
- **Docker Compose**: uno strumento per descrivere, in un unico file YAML
  (`docker-compose.yml`), un **insieme di container che devono lavorare
  insieme** (nel nostro caso: API + SQL Server + Redis + RabbitMQ), invece
  di dover avviare ogni container a mano con comandi separati.
- **Volume**: uno spazio di disco gestito da Docker che **sopravvive** alla
  cancellazione di un container — serve per i dati che non vuoi perdere
  (es. il database).
- **Rete (network)**: quando avvii più container con Docker Compose,
  Docker crea automaticamente una rete privata condivisa tra loro (vedi
  punto 7).
- **Variabile d'ambiente**: un modo per passare configurazione (connection
  string, password, ecc.) a un container **dall'esterno**, senza scriverla
  dentro l'immagine — fondamentale per non mettere segreti nel codice
  sorgente (vedi punto 8).

## 3. I quattro container di questo progetto

```
                     rete privata "bookingsystem_default"
        ┌──────────────────────────────────────────────────────┐
        │                                                        │
        │   bookingsystem-api  ──►  bookingsystem-sqlserver      │
        │   (porta 8080 dentro,        (porta 1433, volume       │
        │    esposta come 5068          "sqlserver-data")        │
        │    sulla tua macchina)                                 │
        │        │                                                │
        │        ├──►  bookingsystem-redis   (porta 6379)         │
        │        │                                                │
        │        └──►  bookingsystem-rabbitmq (porte 5672/15672)  │
        │                                                        │
        └──────────────────────────────────────────────────────┘
```

Tre container usano immagini **già pronte**, scaricate da Docker Hub:
`sqlserver`, `redis`, `rabbitmq`. Solo `api` viene **costruito** in locale
a partire dal codice sorgente di questo repository, tramite il `Dockerfile`.

## 4. Il `Dockerfile` dell'API — build multi-stage

```dockerfile
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore BookingSystem.API/BookingSystem.API.csproj
RUN dotnet publish BookingSystem.API/BookingSystem.API.csproj \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BookingSystem.API.dll"]
```

È diviso in **due stage** (fasi), ognuno a partire da un'immagine diversa
— questa è la tecnica "multi-stage build", pensata per tenere piccola
l'immagine finale:

1. **Build stage** (`FROM mcr.microsoft.com/dotnet/sdk:8.0`): usa
   l'immagine con l'**SDK** completo di .NET (il toolkit che sa compilare,
   circa 800 MB) per copiare tutto il codice sorgente (`COPY . .`) e
   pubblicare l'applicazione compilata (`dotnet publish`, che produce i
   file `.dll` pronti per essere eseguiti, senza il codice sorgente).
2. **Runtime stage** (`FROM mcr.microsoft.com/dotnet/aspnet:8.0`): riparte
   da un'immagine diversa e molto più leggera, che contiene **solo** il
   necessario per *eseguire* un'app ASP.NET Core (non per compilarla).
   `COPY --from=build /app/publish .` copia dentro questa immagine leggera
   *solo* i file già compilati prodotti dallo stage precedente — il codice
   sorgente, l'SDK, tutta la "fase di costruzione" restano fuori
   dall'immagine finale, che risulta quindi molto più piccola (l'immagine
   `aspnet` runtime pesa una frazione di quella `sdk`).

`EXPOSE 8080` dichiara che il processo dentro il container ascolta sulla
porta 8080 (è la porta di default con cui Kestrel, il web server di
ASP.NET Core, ascolta quando non diversamente configurato). `ENTRYPOINT`
è il comando che parte quando il container si avvia.

## 5. `docker-compose.yml` — i quattro servizi

```yaml
services:
  sqlserver:
    image: mcr.microsoft.com/mssql/server:2022-latest
    environment:
      ACCEPT_EULA: "Y"
      SA_PASSWORD: "${SA_PASSWORD}"
    ports:
      - "1433:1433"
    volumes:
      - sqlserver-data:/var/opt/mssql

  redis:
    image: redis:latest
    ports:
      - "6379:6379"

  rabbitmq:
    image: rabbitmq:3-management
    environment:
      RABBITMQ_DEFAULT_USER: "${RABBITMQ_USER}"
      RABBITMQ_DEFAULT_PASS: "${RABBITMQ_PASSWORD}"
    ports:
      - "5672:5672"
      - "15672:15672"

  api:
    build:
      context: .
      dockerfile: BookingSystem.API/Dockerfile
    ports:
      - "5068:8080"
    depends_on:
      - sqlserver
      - redis
      - rabbitmq
    environment:
      ASPNETCORE_ENVIRONMENT: Development
      ConnectionStrings__DefaultConnection: "Server=sqlserver;...;Password=${SA_PASSWORD};..."
      ConnectionStrings__Redis: "redis:6379"
      ConnectionStrings__RabbitMq: "amqp://${RABBITMQ_USER}:${RABBITMQ_PASSWORD}@rabbitmq:5672/"
      Jwt__SecretKey: "${JWT_SECRET_KEY}"
      AdminSeed__Email: "${ADMIN_SEED_EMAIL}"
      AdminSeed__Password: "${ADMIN_SEED_PASSWORD}"

volumes:
  sqlserver-data:
```

Cosa fa ogni chiave:

- **`image`**: quale immagine già pronta scaricare da Docker Hub (usata
  da `sqlserver`, `redis`, `rabbitmq`).
- **`build`**: usato solo da `api`, dice "questa immagine non esiste, va
  costruita" a partire dal `Dockerfile` indicato. `context: .` significa
  "usa la cartella principale del repository come base per i `COPY`
  dentro il Dockerfile" (per questo `COPY . .` nel Dockerfile copia tutto
  il repository, non solo la cartella `BookingSystem.API`).
- **`ports`**: la mappatura `"host:container"`. Es. `"5068:8080"` significa
  "la porta 8080 *dentro* il container (dove Kestrel ascolta davvero) è
  raggiungibile *dalla tua macchina* alla porta 5068" — per questo apri
  `http://localhost:5068/swagger` e non `:8080`. Le porte a sinistra
  (5068, 1433, 6379, 5672, 15672) sono quelle che vedi tu sul tuo PC.
- **`depends_on`**: dice a Docker Compose in che **ordine** avviare i
  container (prima `sqlserver`/`redis`/`rabbitmq`, poi `api`). Nota
  importante: garantisce solo l'ordine di *avvio* del processo dentro il
  container, non che quel servizio sia già pronto ad accettare connessioni
  — è per questo che nel codice C# la connessione a SQL Server usa
  `EnableRetryOnFailure()` (vedi la nota al punto 9).
- **`environment`**: le variabili d'ambiente passate al container. Qui
  vengono lette da `IConfiguration` in `Program.cs` — nessuna di queste
  informazioni è scritta nel codice C# o nell'immagine Docker.
- **`volumes`** (in fondo al file): dichiara `sqlserver-data` come volume
  gestito da Docker, poi usato dal servizio `sqlserver` per salvare i file
  del database in un posto che sopravvive anche se il container viene
  ricreato (vedi punto 6). `redis` e `rabbitmq`, in questo progetto, **non**
  hanno un volume: se il loro container viene ricreato, perdono tutto il
  contenuto — accettabile perché Redis qui è solo una cache "usa e getta"
  (vedi [redis-caching-spiegazione.md](redis-caching-spiegazione.md)) e
  RabbitMQ, in questo progetto didattico, non ha bisogno di conservare i
  messaggi tra un riavvio e l'altro.

## 6. Volumi — perché solo SQL Server ne ha uno

Un container, per definizione, è **effimero**: se lo cancelli (non solo lo
fermi, proprio lo rimuovi con `docker compose down` o `docker rm`), tutto
quello che è stato scritto al suo interno sparisce. Per SQL Server questo
sarebbe un problema serio — perderesti tutte le camere e le prenotazioni
create durante lo sviluppo ogni volta che ricrei il container.

`sqlserver-data:/var/opt/mssql` collega la cartella `/var/opt/mssql`
*dentro* il container (dove SQL Server scrive fisicamente i file del
database) a un volume Docker chiamato `sqlserver-data`, che vive **fuori**
dal container, gestito da Docker stesso. Anche se cancelli e ricrei il
container `sqlserver`, il volume resta e i dati si ritrovano al loro posto.
(Per cancellare anche i dati serve un comando esplicito in più, es.
`docker compose down -v`.)

## 7. La rete — come i container si trovano tra loro

Quando avvii `docker compose up`, Docker crea automaticamente una rete
privata (nel nostro caso si chiamerà `bookingsystem_default`, dal nome
della cartella del progetto) e ci collega dentro tutti i container definiti
nel file. **Dentro questa rete, ogni container può raggiungere gli altri
usando il nome del servizio come se fosse un hostname** — esattamente come
useresti un indirizzo internet.

Per questo, guardando `docker-compose.yml`:

```
ConnectionStrings__DefaultConnection: "Server=sqlserver;..."
ConnectionStrings__Redis: "redis:6379"
ConnectionStrings__RabbitMq: "amqp://...@rabbitmq:5672/"
```

`api` si connette scrivendo `sqlserver`, `redis`, `rabbitmq` — non
`localhost`. `localhost`, *dentro* un container, significa "questo stesso
container", non "la tua macchina" e nemmeno "gli altri container" — è uno
degli errori più comuni quando ci si avvicina a Docker per la prima volta.
Questo è anche il motivo per cui `appsettings.json` (usato quando lanci
`dotnet run` **fuori** da Docker, in locale) usa invece `localhost` per
tutte e tre le connection string: fuori da un container, `localhost` è
davvero la tua macchina, dove giri SQL Server Express/Redis/RabbitMQ
installati direttamente (o, più comodamente, dove hai comunque questi tre
servizi esposti sulle porte host da un `docker compose up` parziale).

## 8. Variabili d'ambiente, `.env` e gestione dei segreti

`docker-compose.yml` non contiene mai un valore vero e proprio per password
o chiavi segrete — usa sempre `${NOME_VARIABILE}`, una sintassi che Docker
Compose sostituisce automaticamente con il valore letto da un file `.env`
nella stessa cartella (Docker Compose lo cerca e lo carica da solo, senza
bisogno di configurazione aggiuntiva).

Nel repository trovi **due file distinti**:

- **`.env.example`** (committato in Git): elenca solo i *nomi* delle
  variabili richieste, senza valori — serve da documentazione per chi
  clona il repository, per sapere cosa deve preparare.
  ```
  SA_PASSWORD=
  JWT_SECRET_KEY=
  ADMIN_SEED_EMAIL=
  ADMIN_SEED_PASSWORD=
  RABBITMQ_USER=
  RABBITMQ_PASSWORD=
  ```
- **`.env`** (⚠️ **non committato**, presente nella riga `.env` di
  `.gitignore`): la copia con i valori *reali* usati in locale. Ogni
  sviluppatore ha il proprio, mai condiviso su Git — è lo stesso principio
  per cui `Jwt:SecretKey` in `appsettings.json` resta vuoto ed è gestito
  con gli User Secrets di ASP.NET Core in locale (vedi la sessione del
  2026-08-06 "Esternalizzazione configurazione e segreti" in
  `context/sessions.md`).

Per far girare il progetto da zero su una macchina nuova, il primo passo è
sempre: copiare `.env.example` in `.env` e riempirlo con valori veri.

## 9. Avviare tutto

```bash
docker compose up --build
```

- `--build` forza Docker a ricostruire l'immagine `api` (serve dopo ogni
  modifica al codice C#; senza `--build`, Docker riusa l'immagine già
  costruita in precedenza, anche se il codice sorgente è cambiato).
- Aggiungendo `-d` (`docker compose up --build -d`), i container partono
  in background e riprendi subito il controllo del terminale.

Cosa succede, in ordine:

1. Docker Compose crea la rete privata e il volume `sqlserver-data` (se
   non esistono già).
2. Costruisce l'immagine `api` seguendo il `Dockerfile` (stage build +
   stage runtime, punto 4).
3. Avvia `sqlserver`, `redis`, `rabbitmq` (nell'ordine indicato da
   `depends_on`), poi `api`.
4. All'avvio, `Program.cs` (righe 121-130) esegue in sequenza:
   `await dbContext.Database.MigrateAsync()` (applica automaticamente
   tutte le migration EF Core pendenti, creando lo schema se il database è
   vuoto) e poi `IdentitySeeder.SeedAsync(...)` (crea i ruoli `Admin`/`User`
   e l'utente amministratore, se non esistono già).
5. Quando nei log compare `Now listening on: http://[::]:8080`, l'API è
   pronta — raggiungibile dal tuo browser su `http://localhost:5068/swagger`.

Comandi utili per il giorno per giorno:

```bash
docker compose logs -f api          # segue i log dell'API in tempo reale
docker compose logs api --tail 50   # ultime 50 righe di log
docker compose ps                   # stato dei container
docker compose stop rabbitmq        # ferma un solo servizio, senza rimuoverlo
docker compose start rabbitmq       # lo riavvia
docker compose restart api          # ferma e riavvia un servizio
docker compose down                 # ferma e RIMUOVE tutti i container (i volumi restano, a meno di -v)
```

## 10. Cosa serve per farlo girare

- **Docker Desktop** installato e avviato (su Windows, include il motore
  Docker vero e proprio più l'integrazione con WSL 2 — Windows Subsystem
  for Linux, il sotto-sistema che permette a Windows di eseguire container
  pensati per Linux).
- Il file `.env` compilato con valori reali (punto 8) — senza, Docker
  Compose sostituirebbe le variabili con stringhe vuote e i servizi
  fallirebbero all'avvio (es. SQL Server richiede una password non vuota).
- Le porte 5068, 1433, 6379, 5672, 15672 libere sulla tua macchina (non
  occupate da altre applicazioni/altri container già avviati).

## 11. Una nota su un problema incontrato (non causato da Docker in sé)

Durante l'ultima sessione (aggiunta di RabbitMQ), al primo avvio è
comparso un errore: `Database 'BookingSystemDb' already exists`. Non è un
problema di Docker, ma un effetto collaterale di `EnableRetryOnFailure()`
(configurato in `Program.cs` per rendere l'app resiliente ai primi istanti
in cui SQL Server non è ancora del tutto pronto): il volume
`sqlserver-data` conteneva già il database creato da sessioni precedenti,
e in quelle condizioni un tentativo di connessione fallito+ritentato ha
provato a ricreare un database che nel frattempo esisteva già. Si è
risolto con un semplice `docker compose restart api` — al secondo tentativo
EF Core ha trovato il database già presente e aggiornato, senza provare a
ricrearlo.

## In sintesi

Docker in questo progetto risolve lo stesso problema in tutti e quattro i
casi: "voglio SQL Server/Redis/RabbitMQ/la mia API già pronti e configurati
allo stesso modo, su qualunque macchina, senza installarli manualmente".
`docker-compose.yml` descrive *cosa* serve e *come* deve essere collegato;
il `Dockerfile` descrive *come si costruisce* la parte che non esiste già
pronta (il nostro codice); `.env` tiene i segreti fuori da Git; il volume
`sqlserver-data` è l'unica eccezione alla regola "i container sono
effimeri", perché per il database serve davvero persistenza.
