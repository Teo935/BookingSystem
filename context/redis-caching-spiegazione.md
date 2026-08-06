# Redis come cache per le stanze — spiegazione

> Scritto il 2026-08-06 su richiesta, per poter rileggere senza dover
> richiedere di nuovo la spiegazione. Riferimento: sessione del 2026-08-06
> "Integrazione Redis come cache (GET /api/rooms)" in `context/sessions.md`.

## 1. Dove vive Redis — `docker-compose.yml`

```yaml
redis:
  image: redis:latest
  container_name: bookingsystem-redis
  ports:
    - "6379:6379"
```
(`docker-compose.yml`, righe 17-22)

Questo è il container che fa girare Redis (**database in-memoria**: tiene i
dati in RAM invece che su disco, per questo è molto più veloce di SQL Server
ma perde tutto se viene riavviato — va bene per una cache, che per
definizione è "usa e getta"). Redis non ha bisogno di un `Dockerfile` come
invece serve per `api`: l'immagine ufficiale è già pronta su Docker Hub.

`redis` è stato aggiunto anche ai `depends_on` del servizio `api` (riga
34-36), così Docker Compose lo avvia prima dell'API — garantisce solo
l'ordine di *avvio*, non che Redis sia già pronto ad accettare connessioni,
ma per un servizio che parte in pochi secondi come Redis non è un problema
pratico in questo progetto.

## 2. Come l'API sa dove si trova Redis — configurazione

- `BookingSystem.API/appsettings.json`, riga 4: `"ConnectionStrings:Redis":
  "localhost:6379"` — usato quando lanci `dotnet run` in locale, fuori da
  Docker.
- `BookingSystem.API/appsettings.json`, righe 16-18: sezione `Caching` con
  `RoomsCacheDurationSeconds: 60` — per quanti secondi un risultato resta
  valido in cache prima di scadere da solo.
- `docker-compose.yml`, riga 42: `ConnectionStrings__Redis: "redis:6379"`
  per il servizio `api` — nota `redis` al posto di `localhost`: dentro la
  rete privata creata da Docker Compose, ogni servizio raggiunge gli altri
  usando **il nome del servizio come hostname** (esattamente come già
  succede con `sqlserver` per il database). Il doppio underscore `__` è la
  convenzione di ASP.NET Core per dire "questa variabile d'ambiente
  corrisponde alla chiave annidata `ConnectionStrings:Redis`".

Nessun valore hardcoded nel codice C#: sia la connection string sia la
durata della cache arrivano da `IConfiguration`.

## 3. Il "contratto" — `ICacheService` (Application)

`BookingSystem.Application/Interfaces/ICacheService.cs` (righe 3-8):

```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan expiration);
    Task RemoveAsync(string key);
}
```

Non menziona Redis da nessuna parte — è pensato così apposta: il livello
Application (dove vive la logica di business) deve poter dire "voglio una
cache" senza sapere *quale* tecnologia c'è dietro. Stesso identico
principio con cui `IRoomRepository` disaccoppia i Service da Entity
Framework Core (Dependency Inversion, l'ultima delle 5 regole SOLID).

## 4. L'implementazione reale — `RedisCacheService` (Infrastructure)

`BookingSystem.Infrastructure/Caching/RedisCacheService.cs` (righe 7-38):
implementa `ICacheService` usando `IDistributedCache` — un'interfaccia già
fornita da ASP.NET Core per "cache condivisa esterna al processo" (a
differenza di `IMemoryCache`, che vive dentro la RAM del singolo processo
API e sparisce se il container si riavvia). `IDistributedCache` di per sé è
generico, non sa nulla di Redis: diventa concretamente "Redis" solo grazie
a come viene registrato in `Program.cs` (punto 7 sotto).

`RedisCacheService` fa solo un lavoro in più: serializza/deserializza i
dati in JSON con `System.Text.Json`, perché `IDistributedCache` lavora solo
con stringhe/byte, non con oggetti C# (`GetAsync`/`SetAsync`, righe 16-31).

## 5. La configurazione della durata — `CacheSettings` (Infrastructure)

`BookingSystem.Infrastructure/Caching/CacheSettings.cs`: una piccola classe
con `RoomsCacheDurationSeconds` (default 60, sovrascrivibile da
`appsettings.json`/variabili d'ambiente). Stesso pattern già usato da
`JwtSettings` e `AdminSeedOptions`.

## 6. Il cuore della logica — `CachedRoomService` (Infrastructure)

`BookingSystem.Infrastructure/Caching/CachedRoomService.cs` (righe 9-70):
implementa la stessa interfaccia `IRoomService` che implementa anche il
vero `RoomService`, ma **avvolge** un'istanza di `RoomService` invece di
duplicarne la logica — pattern "decorator". Riceve nel costruttore sia il
`RoomService` vero sia l'`ICacheService`:

- `GetAllRoomsAsync()` (riga 24) — controlla prima la cache; se assente,
  delega al `RoomService` vero e salva il risultato in cache.
- `CreateRoomAsync()` (riga 35), `UpdateRoomAsync()` (riga 44),
  `DeleteRoomAsync()` (riga 53) — delegano sempre al `RoomService` vero, e
  **solo se l'operazione riesce** invalidano la cache (`RemoveAsync`).
- `GetRoomAsync()` (riga 62) — passa dritto al `RoomService` vero, nessuna
  cache coinvolta (fuori dallo scope richiesto: solo la lista completa
  viene messa in cache).

`RoomService` (il service originale con la validazione di business) **non è
stato toccato** — rispetta l'Open/Closed Principle e nessun test esistente
si è rotto.

## 7. Il collante — `Program.cs`

Righe 78-94:

```csharp
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("Caching"));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
// ↑ qui IDistributedCache diventa concretamente "parla con Redis"

builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// ... altre registrazioni ...

builder.Services.AddScoped<RoomService>();           // il service vero, con il suo tipo concreto
builder.Services.AddScoped<IRoomService>(sp => new CachedRoomService(
    sp.GetRequiredService<RoomService>(),             // ← il vero RoomService, iniettato nel decorator
    sp.GetRequiredService<ICacheService>(),
    sp.GetRequiredService<IOptions<CacheSettings>>()));
```

Qui avviene la Dependency Injection (DI): quando qualcosa chiede
`IRoomService` (come fa `RoomsController`), ASP.NET Core non gli dà più
direttamente `RoomService` — gli dà `CachedRoomService`, che a sua volta
tiene dentro di sé il vero `RoomService`.

## 8. `RoomsController` — invariato

Il controller continua a dichiarare `IRoomService _roomService` nel
costruttore, esattamente come prima di questa modifica. Non sa e non deve
sapere che dietro c'è un decorator con Redis — è proprio l'obiettivo della
Dependency Injection: il controller dipende da un'astrazione, non da
un'implementazione concreta.

## Il flusso completo, passo per passo

**`GET /api/rooms`:**
```
RoomsController → IRoomService (= CachedRoomService)
                     ↓
          CachedRoomService.GetAllRoomsAsync()
                     ↓
          ICacheService.GetAsync<List<Room>>("rooms:all")
                     ↓
          RedisCacheService → IDistributedCache → Redis (container)
```
- Cache **HIT** → torna subito il valore da Redis, SQL Server non viene mai
  interpellato.
- Cache **MISS** → `CachedRoomService` chiama il vero
  `RoomService.GetAllRoomsAsync()` → che interroga SQL Server tramite
  `IRoomRepository` → il risultato viene salvato in Redis (scadenza 60
  secondi di default) e poi restituito.

**`POST /api/rooms` (o `PUT`/`DELETE`):**
```
RoomsController → CachedRoomService.CreateRoomAsync()
                     ↓
          delega al vero RoomService (scrive su SQL Server)
                     ↓
          se successo → ICacheService.RemoveAsync("rooms:all")
                     (cancella la chiave da Redis, così la prossima GET
                      rilegge i dati freschi da SQL Server)
```

In breve: **SQL Server resta sempre l'unica fonte di verità**, Redis è solo
una copia temporanea dei risultati di lettura, buttata via ogni volta che i
dati cambiano.

## Verificato end-to-end

Con `docker compose up --build`: prima `GET /api/rooms` → cache miss,
valore salvato su Redis (ispezionato con `redis-cli HGETALL rooms:all`);
`POST /api/rooms` → cache invalidata; `GET` successiva → ripopolata con la
nuova stanza; una richiesta in cache HIT confermata (via log dell'API) **non
esegue alcuna query SQL** sulla tabella `Rooms`.

## Nota aperta

L'immagine Docker usa il tag `redis:latest`; per riproducibilità in un
contesto più vicino alla produzione converrebbe fissare una versione
esplicita (es. `redis:7-alpine`), così una futura `docker pull` non cambia
versione senza preavviso.
