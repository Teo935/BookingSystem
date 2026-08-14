# Refresh Token + Redis — spiegazione dettagliata

Data: 2026-08-14. Questo file spiega **cosa fa ogni file** toccato per aggiungere
Refresh Token (con Redis) al login, e **come funziona il codice al suo interno**,
riga per riga dove serve. È pensato come riferimento da rileggere in futuro,
non solo come cronaca — per la cronaca della sessione vedi `context/sessions.md`.

## Concetti di base (per chi legge senza contesto)

- **Access Token**: il JWT (JSON Web Token — un token firmato che contiene
  informazioni leggibili, tipo "chi sei" e "che ruolo hai") che il client manda
  ad ogni richiesta protetta nell'header `Authorization: Bearer <token>`. Ha
  vita breve (60 minuti in questo progetto) apposta: se viene rubato, il danno
  è limitato nel tempo.
- **Refresh Token**: un token separato, di vita più lunga (7 giorni), che serve
  a un solo scopo: ottenere un nuovo Access Token senza dover rifare login con
  email/password. Se l'Access Token scade, il client manda il Refresh Token a
  `/api/auth/refresh` e ne riceve uno nuovo.
- **Perché il Refresh Token non è anch'esso un JWT**: un JWT è **autovalidante**
  — il server lo verifica solo controllando la firma, senza guardare un database.
  Questo è un pregio per l'Access Token (veloce, nessuna query ad ogni richiesta),
  ma è un problema per un token che deve poter essere **revocato** (es. al
  logout): un JWT firmato resta valido finché non scade, il server non ha modo
  di "cancellarlo" a meno di tenere una lista nera. Per questo il Refresh Token
  qui è una **stringa casuale opaca**, salvata in Redis: revocarla significa
  semplicemente cancellarla da Redis.
- **Redis**: un database che tiene i dati in RAM (memoria veloce) invece che su
  disco, già usato in questo progetto per la cache delle stanze e per il rate
  limiting (limitare quante richieste può fare un utente in un certo tempo).
  Qui viene riusato per salvare i Refresh Token.
- **TTL (Time To Live)**: per quanto tempo un dato salvato in Redis resta valido
  prima di essere cancellato automaticamente. Il Refresh Token ha TTL 7 giorni:
  dopo, Redis lo elimina da solo, senza bisogno di codice che lo controlli.
- **Token rotation (rotazione del token)**: ogni volta che un Refresh Token
  viene usato per ottenere un nuovo Access Token, viene **immediatamente
  invalidato** e ne viene emesso uno nuovo. Serve a limitare il danno se un
  Refresh Token viene rubato: può essere usato una volta sola prima di diventare
  inutile (se il legittimo proprietario lo riusa dopo che è stato rubato e già
  usato dall'attaccante, il server vede un token già consumato e può insospettirsi
  — anche se in questo progetto, per restare semplice, non implementiamo ancora
  quella rilevazione, solo la rotazione base).

---

## File nuovi

### 1. `BookingSystem.Application/Interfaces/IRefreshTokenStore.cs`

```csharp
public interface IRefreshTokenStore
{
    Task StoreAsync(string token, string userId, TimeSpan ttl);
    Task<string?> GetUserIdAsync(string token);
    Task RemoveAsync(string token);
}
```

**A cosa serve**: è un'**interfaccia** (un contratto: dice "chi la implementa
deve avere questi tre metodi", senza dire come). Vive nel progetto
`BookingSystem.Application`, il livello che contiene la logica di business, e
che **non deve mai sapere che sotto c'è Redis** — potrebbe domani diventare
SQL Server, un altro database, non cambia nulla per chi la usa. Questo è il
principio "Dependency Inversion" (la "D" di SOLID): il livello business dipende
da un'astrazione, non da un dettaglio tecnico concreto.

**Come funziona**: tre operazioni, tutte asincrone (`Task`/`Task<T>` — significano
"questa operazione richiede tempo, es. una chiamata di rete a Redis, e il resto
del codice può continuare a fare altro nel frattempo invece di bloccarsi ad
aspettare"):
- `StoreAsync` — salva un Refresh Token, associato a un `userId`, con una
  scadenza (`ttl`).
- `GetUserIdAsync` — dato un token, ritorna l'id dell'utente a cui appartiene,
  oppure `null` se il token non esiste (mai salvato, oppure scaduto, oppure
  già rimosso).
- `RemoveAsync` — elimina un token (usato sia in fase di rotazione sia al
  logout).

### 2. `BookingSystem.Infrastructure/Identity/RedisRefreshTokenStore.cs`

```csharp
public class RedisRefreshTokenStore : IRefreshTokenStore
{
    private const string KeyPrefix = "refreshtoken:";
    private readonly IConnectionMultiplexer _redis;

    public RedisRefreshTokenStore(IConnectionMultiplexer redis)
    {
        _redis = redis;
    }

    public Task StoreAsync(string token, string userId, TimeSpan ttl)
    {
        var db = _redis.GetDatabase();
        return db.StringSetAsync(KeyPrefix + token, userId, ttl);
    }

    public async Task<string?> GetUserIdAsync(string token)
    {
        var db = _redis.GetDatabase();
        var value = await db.StringGetAsync(KeyPrefix + token);
        return value.IsNullOrEmpty ? null : value.ToString();
    }

    public Task RemoveAsync(string token)
    {
        var db = _redis.GetDatabase();
        return db.KeyDeleteAsync(KeyPrefix + token);
    }
}
```

**A cosa serve**: è l'implementazione **concreta** di `IRefreshTokenStore` —
il pezzo che sa davvero parlare con Redis. Vive in `BookingSystem.Infrastructure`,
il livello che contiene tutti i dettagli tecnici (database, cache, provider
esterni).

**Come funziona**:
- `IConnectionMultiplexer` è la connessione a Redis (una singola connessione
  condivisa, aperta una volta sola all'avvio dell'applicazione — non se ne apre
  una nuova per ogni richiesta, sarebbe uno spreco). È lo stesso oggetto già
  usato nel progetto da `RedisRateLimiter` per il rate limiting.
- `_redis.GetDatabase()` non apre una nuova connessione: è un'operazione
  leggerissima che ritorna un "puntatore" logico al database Redis (Redis ha
  16 database numerati per default, `GetDatabase()` senza argomenti prende il
  database 0).
- Ogni Refresh Token viene salvato con una chiave tipo `refreshtoken:AbCdEf123...`
  (prefisso + il token stesso) e come valore l'`userId` proprietario. Il
  prefisso `refreshtoken:` serve a distinguere queste chiavi dalle altre già
  presenti in Redis (es. `rooms:all` per la cache, `ratelimit:login:1.2.3.4`
  per il rate limiting) — tutte condividono lo stesso Redis, i prefissi le
  tengono separate.
- `StringSetAsync(key, value, ttl)` — comando Redis nativo: salva
  `valore` sotto `chiave`, con scadenza automatica dopo `ttl`. Passato il
  tempo, Redis la cancella da solo, non serve altro codice.
- `StringGetAsync(key)` — legge il valore. Se la chiave non esiste (mai
  creata, o scaduta, o cancellata), StackExchange.Redis (la libreria .NET per
  parlare con Redis) ritorna un valore speciale "nullo" (`RedisValue.Null`),
  controllato con `.IsNullOrEmpty`.
- `KeyDeleteAsync(key)` — cancella la chiave subito, prima della scadenza
  naturale. Usato sia quando un token viene "ruotato" (il vecchio va tolto)
  sia al logout. È un'operazione **idempotente**: cancellare una chiave che
  non esiste già non dà errore, semplicemente non fa nulla — per questo il
  logout può sempre rispondere "fatto" anche se il token era già scaduto.

### 3. `BookingSystem.Application/DTOs/RefreshTokenRequest.cs`

```csharp
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}
```

**A cosa serve**: è un DTO (Data Transfer Object — una classe che esiste solo
per rappresentare la forma di una richiesta/risposta HTTP, senza logica dentro).
Rappresenta il corpo JSON che il client deve mandare sia a `/api/auth/refresh`
sia a `/api/auth/logout`: `{ "refreshToken": "..." }`. Riusato per entrambi gli
endpoint perché la forma richiesta è identica — evita di duplicare la stessa
classe con due nomi diversi.

### 4. `BookingSystem.Tests/Identity/RedisRefreshTokenStoreTests.cs`

**A cosa serve**: verifica che `RedisRefreshTokenStore` chiami i comandi Redis
giusti, con i parametri giusti, **senza usare un Redis vero** — al suo posto
c'è un oggetto finto (un "mock", costruito con la libreria Moq) che si
comporta come Redis solo per la durata del test, e permette di controllare
esattamente cosa è stato chiamato.

**Come funziona**: si mocka `IConnectionMultiplexer` in modo che
`.GetDatabase()` ritorni un `IDatabase` anch'esso finto; su quel database
finto si "programmano" le risposte (es. "quando chiami `StringGetAsync` con
questa chiave, rispondi con questo valore") e poi si verifica che i metodi
giusti siano stati chiamati il numero di volte atteso. 4 test: salvataggio con
TTL corretto, lettura quando il token esiste, lettura quando non esiste
(ritorna `null`), cancellazione.

### 5. `BookingSystem.Tests/Identity/AuthServiceTests.cs`

**A cosa serve**: prima di questa sessione, `AuthService` non aveva **nessun**
test — un buco di copertura preesistente, non causato da questa modifica, ma
colmato approfittando del fatto che si stava comunque toccando quel file.
Verifica tutta la logica nuova (login genera e salva il refresh token, register
non lo genera, refresh ruota correttamente il token, logout lo rimuove) più
un minimo dei casi già esistenti (credenziali sbagliate).

**Come funziona — la parte più delicata**: `AuthService` dipende da
`UserManager<ApplicationUser>`, una classe di ASP.NET Core Identity (la
libreria di autenticazione già usata nel progetto) che non è un'interfaccia,
ma i suoi metodi pubblici sono dichiarati `virtual` (sovrascrivibili) — per
questo Moq riesce comunque a "fingerla", creandone una versione finta che
intercetta le chiamate a `FindByEmailAsync`, `CheckPasswordAsync`, ecc. Per
costruirla serve comunque passare un finto "store" (`IUserStore<ApplicationUser>`)
al costruttore, anche se non verrà mai usato davvero:

```csharp
var store = new Mock<IUserStore<ApplicationUser>>();
return new Mock<UserManager<ApplicationUser>>(store.Object, null!, null!, null!, null!, null!, null!, null!, null!);
```

Per `JwtTokenGenerator`, invece, si usa la versione **reale** (non finta):
i suoi metodi non sono `virtual`, quindi Moq non potrebbe comunque fingerli, e
non c'è alcun motivo di modificarlo solo per renderlo "testabile" — generare
un vero JWT con una chiave segreta di test è semplice, veloce, e in più
verifica realmente che il token generato sia valido.

---

## File modificati

### 6. `BookingSystem.Infrastructure/Identity/JwtSettings.cs`

```csharp
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpirationMinutes { get; set; }
    public int RefreshTokenExpirationDays { get; set; }   // <-- aggiunto
}
```

**A cosa serve**: è la classe "contenitore" delle impostazioni JWT, letta da
`appsettings.json` all'avvio (vedi punto 11). Aggiunta una sola proprietà:
per quanti **giorni** un Refresh Token resta valido. Nota l'unità di misura
diversa da `ExpirationMinutes` (minuti per l'Access Token, giorni per il
Refresh Token) — riflette che i due token hanno vite molto diverse in scala.

### 7. `BookingSystem.Infrastructure/Identity/JwtTokenGenerator.cs`

Due aggiunte alla classe che già generava l'Access Token:

```csharp
public string GenerateRefreshToken()
{
    var bytes = RandomNumberGenerator.GetBytes(64);
    return Convert.ToBase64String(bytes)
        .Replace('+', '-')
        .Replace('/', '_')
        .TrimEnd('=');
}

public TimeSpan RefreshTokenExpiration => TimeSpan.FromDays(_settings.RefreshTokenExpirationDays);
```

**A cosa serve `GenerateRefreshToken`**: crea il valore del Refresh Token —
64 byte (512 bit) casuali generati da `RandomNumberGenerator`, la classe
.NET per numeri casuali **crittograficamente sicuri** (a differenza di
`Random`, che è prevedibile e non va mai usato per token/password — se un
attaccante riuscisse a indovinare il seed potrebbe prevedere i token futuri).

**Come funziona la codifica**: i byte casuali vengono convertiti in una
stringa leggibile con Base64 (`Convert.ToBase64String`), ma Base64 standard usa
i caratteri `+`, `/` e `=`, che non sono sicuri da mettere direttamente in un
URL o in un header HTTP senza escaping. Le tre righe `.Replace`/`.TrimEnd`
trasformano il Base64 standard in **Base64Url** (variante pensata apposta per
URL/HTTP), sostituendo `+`→`-`, `/`→`_` e togliendo il padding finale `=`.
Risultato: una stringa tipo `0xK_jb34-4zvrRidD9Ck1IqP5lurnE1xKTHN8eGN9EHc3B7c1Uydbt3jw434StzC1UV_06SJaVuqbtePJA5vRw`.

**A cosa serve `RefreshTokenExpiration`**: è una comodità — invece di far
ripetere a `AuthService` la conversione "giorni configurati → `TimeSpan`" ogni
volta che serve, la calcola qui una volta, vicino a dove vive già
`ExpirationMinutes`.

### 8. `BookingSystem.Application/DTOs/AuthResponse.cs`

```csharp
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public string? RefreshToken { get; set; }   // <-- aggiunto, nullable
    public string Email { get; set; } = string.Empty;
    public IEnumerable<string> Roles { get; set; } = Array.Empty<string>();
}
```

**A cosa serve**: è la forma della risposta JSON che il client riceve da
`Login`, `Refresh` e `Register`. `RefreshToken` è dichiarato **nullable**
(`string?`, cioè può valere `null`) perché `Register` — per scelta di design,
vedi sotto — non lo genera: nella risposta di registrazione questo campo
arriva come `"refreshToken": null`.

### 9. `BookingSystem.Application/Interfaces/IAuthService.cs`

```csharp
public interface IAuthService
{
    Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request);
    Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken);   // <-- aggiunto
    Task LogoutAsync(string refreshToken);   // <-- aggiunto
}
```

**A cosa serve**: è il contratto che il Controller usa per parlare con la
logica di autenticazione, senza sapere come è implementata (stesso principio
di `IRefreshTokenStore`, punto 1). Le firme di `RefreshAsync` seguono lo
stesso stile già usato da `Register`/`Login`: una **tupla** con tre valori
— `Success` (andata bene sì/no), `Error` (messaggio se no), `Response` (il
risultato se sì). `LogoutAsync` invece ritorna solo `Task` (nessun valore):
il logout in questo progetto è pensato per **non poter mai fallire in modo
significativo per il chiamante** — cancellare un token che non esiste è
comunque un successo dal punto di vista di chi chiama ("non hai più quella
sessione attiva", vero sia che il token esistesse sia che no) — quindi non
serve nessun `bool`/`enum` per distinguere degli esiti che, per chi chiama,
sono in pratica lo stesso risultato.

### 10. `BookingSystem.Infrastructure/Identity/AuthService.cs`

Il cuore della funzionalità. Riporto la classe per intero e la spiego a blocchi.

```csharp
public class AuthService : IAuthService
{
    private const string DefaultRole = Roles.User;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly JwtTokenGenerator _tokenGenerator;
    private readonly IRefreshTokenStore _refreshTokenStore;   // <-- nuova dipendenza

    public AuthService(UserManager<ApplicationUser> userManager, JwtTokenGenerator tokenGenerator, IRefreshTokenStore refreshTokenStore)
    {
        _userManager = userManager;
        _tokenGenerator = tokenGenerator;
        _refreshTokenStore = refreshTokenStore;
    }
```

`AuthService` ora dipende anche da `IRefreshTokenStore` — nota: dipende
dall'**interfaccia**, non da `RedisRefreshTokenStore` direttamente. Questo
oggetto le viene "iniettato" (Dependency Injection, DI — un meccanismo per
cui una classe non crea da sola le sue dipendenze, ma le riceve già pronte
dall'esterno, in questo caso da `Program.cs`, vedi punto 12) dal costruttore.

```csharp
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RegisterAsync(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var error = string.Join("; ", result.Errors.Select(e => e.Description));
            return (false, error, null);
        }

        await _userManager.AddToRoleAsync(user, DefaultRole);

        var response = BuildAuthResponse(user, new[] { DefaultRole });   // <-- NON genera refresh token
        return (true, null, response);
    }
```

`RegisterAsync` è **quasi identico a prima**: l'unica differenza è che ora
chiama esplicitamente `BuildAuthResponse` (senza refresh token) invece del
metodo con refresh token — per la scelta di non toccare il comportamento di
registrazione, vedi sotto "Perché Register non genera un refresh token".

```csharp
    public async Task<(bool Success, string? Error, AuthResponse? Response)> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return (false, "Invalid email or password.", null);

        var passwordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid) return (false, "Invalid email or password.", null);

        var roles = await _userManager.GetRolesAsync(user);
        var response = await BuildAuthResponseWithRefreshTokenAsync(user, roles);   // <-- unica riga cambiata
        return (true, null, response);
    }
```

`LoginAsync` è cambiato di una sola riga rispetto a prima: chiamava
`BuildAuthResponse`, ora chiama `BuildAuthResponseWithRefreshTokenAsync` (vedi
sotto), che fa tutto quello che faceva prima **più** la generazione/salvataggio
del refresh token. Tutta la logica di validazione (utente esiste? password
corretta?) è **invariata** — il messaggio di errore generico
`"Invalid email or password."` in entrambi i casi (utente non trovato o
password sbagliata) non è cambiato: serve a non rivelare a un attaccante se
un'email è registrata o no ("user enumeration").

```csharp
    public async Task<(bool Success, string? Error, AuthResponse? Response)> RefreshAsync(string refreshToken)
    {
        var userId = await _refreshTokenStore.GetUserIdAsync(refreshToken);
        if (userId == null)
        {
            return (false, "Invalid or expired refresh token.", null);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            await _refreshTokenStore.RemoveAsync(refreshToken);
            return (false, "Invalid or expired refresh token.", null);
        }

        // Token rotation: il vecchio refresh token viene invalidato prima che il nuovo venga emesso.
        await _refreshTokenStore.RemoveAsync(refreshToken);

        var roles = await _userManager.GetRolesAsync(user);
        var response = await BuildAuthResponseWithRefreshTokenAsync(user, roles);
        return (true, null, response);
    }
```

Questo è il metodo **completamente nuovo** che implementa `/api/auth/refresh`.
Passo per passo:
1. **Cerca il token in Redis** (`GetUserIdAsync`). Se non lo trova (mai
   esistito, scaduto naturalmente per il TTL, o già usato/rimosso in
   precedenza), fallisce subito con un messaggio generico.
2. **Recupera l'utente** a partire dall'id trovato in Redis
   (`_userManager.FindByIdAsync`). Questo controllo in più gestisce un caso
   limite: un token valido in Redis il cui utente è però stato nel frattempo
   eliminato dal database (raro, ma possibile) — in quel caso il token
   "orfano" viene ripulito da Redis (`RemoveAsync`) e la richiesta fallisce
   comunque, con lo stesso messaggio generico (nessuna informazione in più
   data a chi ha fatto la richiesta).
3. **Rimuove il vecchio token** (`RemoveAsync`) — **prima** di generarne uno
   nuovo. Questa riga, insieme alla generazione del nuovo token dentro
   `BuildAuthResponseWithRefreshTokenAsync`, è la "rotazione": il token appena
   usato non funzionerà mai più, anche se qualcuno ne avesse fatto una copia.
4. **Genera nuovo Access Token + nuovo Refresh Token** e li ritorna, esattamente
   come fa il login.

```csharp
    public Task LogoutAsync(string refreshToken)
    {
        return _refreshTokenStore.RemoveAsync(refreshToken);
    }
```

`LogoutAsync` è il metodo più semplice di tutti: cancella il token da Redis e
basta. Non c'è modo di "invalidare" l'Access Token già emesso (è un JWT,
resta valido finché non scade naturalmente, come spiegato sopra) — quello che
il logout garantisce è che **non si potrà più ottenere un nuovo Access Token**
con quel Refresh Token, quindi la sessione si spegne comunque entro
al massimo 60 minuti (la durata dell'Access Token).

```csharp
    private AuthResponse BuildAuthResponse(ApplicationUser user, IEnumerable<string> roles)
    {
        var rolesList = roles.ToList();
        var (token, expiresAt) = _tokenGenerator.GenerateToken(user, rolesList);

        return new AuthResponse
        {
            Token = token,
            ExpiresAt = expiresAt,
            Email = user.Email ?? string.Empty,
            Roles = rolesList
        };
    }

    private async Task<AuthResponse> BuildAuthResponseWithRefreshTokenAsync(ApplicationUser user, IEnumerable<string> roles)
    {
        var response = BuildAuthResponse(user, roles);

        var refreshToken = _tokenGenerator.GenerateRefreshToken();
        await _refreshTokenStore.StoreAsync(refreshToken, user.Id, _tokenGenerator.RefreshTokenExpiration);
        response.RefreshToken = refreshToken;

        return response;
    }
```

`BuildAuthResponse` è il metodo privato **originale**, non toccato: costruisce
la risposta con solo l'Access Token (usato ancora oggi da `Register`).
`BuildAuthResponseWithRefreshTokenAsync` è nuovo e **lo richiama** (evitando di
duplicare la logica dell'Access Token): genera in più un Refresh Token, lo
salva in Redis con `StoreAsync` (passando l'id dell'utente e la scadenza già
calcolata da `JwtTokenGenerator.RefreshTokenExpiration`), e lo aggiunge alla
risposta prima di ritornarla. Usato da `Login` e da `Refresh`.

**Perché Register non genera un refresh token**: perché la richiesta esplicita
era "modifica il **login** esistente" — `Register` resta con lo stesso
comportamento di prima (solo Access Token). È una scelta facilmente
reversibile in futuro se si vorrà che anche la registrazione "logghi dentro"
subito l'utente con una sessione completa.

### 11. `BookingSystem.API/Controllers/AuthController.cs`

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
{
    var (success, error, response) = await _authService.RefreshAsync(request.RefreshToken);

    if (!success) return Unauthorized(new { error });

    return Ok(response);
}

[HttpPost("logout")]
public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
{
    await _authService.LogoutAsync(request.RefreshToken);

    return NoContent();
}
```

**A cosa servono**: sono i due nuovi endpoint HTTP. Notare quanto sono
"stupidi" di proposito — nessuna riga parla di Redis, di token, di scadenze:
il Controller si limita a **ricevere la richiesta HTTP, chiamare il Service, e
tradurre il risultato in una risposta HTTP**. Questo rispetta il vincolo
richiesto ("non introdurre logica Redis nei Controller") ed è lo stesso
pattern già usato da `Login`/`Register` in questo file, e da tutti gli altri
Controller del progetto (`RoomsController`, `BookingsController`).

- `Refresh`: se `RefreshAsync` fallisce (token invalido/scaduto), risponde
  `401 Unauthorized` con un messaggio di errore — stesso status code già usato
  da `Login` quando le credenziali sono sbagliate, coerente semanticamente
  ("non sei autenticato/autorizzato"). Se va bene, `200 OK` con la nuova
  coppia di token.
- `Logout`: risponde sempre `204 No Content` (richiesta riuscita, nessun corpo
  da ritornare) — non c'è un ramo di fallimento visibile al client, per le
  ragioni spiegate al punto 9 (`LogoutAsync` non può "fallire" in un modo che
  interessi al chiamante).

Nessuno dei due endpoint ha `[Authorize]`: sono raggiungibili senza un Access
Token valido, il che ha senso — il `Refresh` esiste proprio per il caso in cui
l'Access Token sia scaduto, e il `Logout` deve funzionare anche se il client
ha già perso un Access Token valido. La sicurezza sta nel fatto che solo chi
possiede il Refresh Token (una stringa casuale di 512 bit, impossibile da
indovinare) può usarli.

### 12. `BookingSystem.API/Program.cs`

Aggiunta una sola riga nella sezione di configurazione delle dipendenze:

```csharp
builder.Services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();
```

**A cosa serve**: è la registrazione nel contenitore di Dependency Injection
(DI) — dice "quando una classe (come `AuthService`) chiede nel costruttore un
`IRefreshTokenStore`, dagli un'istanza di `RedisRefreshTokenStore`". Senza
questa riga, l'applicazione non si avvierebbe (errore "impossibile risolvere
il servizio").

**Perché `AddSingleton`** (una sola istanza condivisa per tutta la vita
dell'applicazione, non una nuova ad ogni richiesta): `RedisRefreshTokenStore`
non ha stato proprio (nessun campo che cambia tra una chiamata e l'altra,
solo la connessione Redis condivisa), quindi è sicuro riusare sempre lo
stesso oggetto — esattamente come già fatto per `RedisRateLimiter` e
`RedisCacheService` in questo stesso file, stesso pattern.

Non è stata registrata nessuna nuova connessione Redis: `IConnectionMultiplexer`
era **già** registrato come singleton (usato da `RedisRateLimiter`), e
`RedisRefreshTokenStore` lo riceve in ingresso allo stesso modo — nessuna
connessione in più aperta verso Redis.

### 13. `BookingSystem.API/appsettings.json`

```json
"Jwt": {
  "SecretKey": "",
  "Issuer": "BookingSystem.API",
  "Audience": "BookingSystem.Client",
  "ExpirationMinutes": 60,
  "RefreshTokenExpirationDays": 7   // <-- aggiunto
}
```

**A cosa serve**: è il valore di configurazione che `Program.cs` legge
all'avvio (`builder.Configuration.GetSection("Jwt").Get<JwtSettings>()`) e
popola dentro l'oggetto `JwtSettings` (punto 6). `7` è il valore di default:
per cambiarlo basta modificare questo numero (o, in Docker, passare la
variabile d'ambiente `Jwt__RefreshTokenExpirationDays`, con la stessa
convenzione già usata per le altre chiavi `Jwt__*`) — nessuna modifica al
codice richiesta, come da requisito "nessun valore hardcoded, usa
`IConfiguration`". Non è un segreto (a differenza di `SecretKey`), quindi
resta in chiaro nel file, come già `ExpirationMinutes`.

---

## Riassunto del flusso end-to-end

```
LOGIN
  Client → POST /api/auth/login {email, password}
  AuthService.LoginAsync:
    1. Verifica credenziali (invariato)
    2. Genera Access Token (JWT, 60 min)
    3. Genera Refresh Token (stringa casuale, 512 bit)
    4. Salva in Redis: refreshtoken:{token} = userId, TTL 7 giorni
  Client ← 200 { token, expiresAt, refreshToken, email, roles }

REFRESH (quando l'Access Token sta per scadere o è scaduto)
  Client → POST /api/auth/refresh {refreshToken}
  AuthService.RefreshAsync:
    1. Cerca refreshtoken:{token} in Redis → userId (o fallisce con 401)
    2. Recupera l'utente da quell'userId
    3. Rimuove il vecchio token da Redis (rotation)
    4. Genera nuovo Access Token + nuovo Refresh Token, li salva
  Client ← 200 { nuovo token, nuova scadenza, nuovo refreshToken, email, roles }
  (il vecchio refreshToken non funziona più da questo momento)

LOGOUT
  Client → POST /api/auth/logout {refreshToken}
  AuthService.LogoutAsync:
    1. Rimuove refreshtoken:{token} da Redis
  Client ← 204 No Content
  (l'Access Token già emesso resta valido fino alla sua naturale scadenza,
   max 60 minuti, ma non può più essere rinnovato con quel refreshToken)
```
