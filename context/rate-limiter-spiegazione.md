1. Il contratto — IRateLimiter (Application)

BookingSystem.Application/Interfaces/IRateLimiter.cs

public interface IRateLimiter
{
Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window);
}
Un solo metodo: "data una chiave, posso fare un'altra richiesta entro il limite limit nella finestra temporale window?". Non sa nulla di Redis — stesso principio di ICacheService: il livello Application parla solo di concetti astratti.

2. La configurazione — RateLimitPolicy (Infrastructure)

BookingSystem.Infrastructure/RateLimiting/RateLimitOptions.cs

public class RateLimitPolicy
{
public int Limit { get; set; }
public int WindowSeconds { get; set; }
}
Solo un contenitore dati: quante richieste (Limit) in quanti secondi (WindowSeconds). Letto da appsettings.json, sezione RateLimiting:

"RateLimiting": {
"Login": { "Limit": 5, "WindowSeconds": 60 },
"Register": { "Limit": 5, "WindowSeconds": 3600 },
"CreateBooking": { "Limit": 10, "WindowSeconds": 60 }
}
Ogni chiave di primo livello (Login, Register, CreateBooking) è il nome di una "policy" — un insieme di regole con nome, così puoi avere soglie diverse per endpoint diversi.

3. L'implementazione reale — RedisRateLimiter (Infrastructure)

BookingSystem.Infrastructure/RateLimiting/RedisRateLimiter.cs

public async Task<bool> IsAllowedAsync(string key, int limit, TimeSpan window)
{
var db = \_redis.GetDatabase();
var count = await db.StringIncrementAsync(key); // INCR su Redis
if (count == 1)
{
await db.KeyExpireAsync(key, window); // scadenza solo al primo hit
}
return count <= limit;
}
Questo è l'algoritmo vero e proprio, chiamato "fixed-window counter" (contatore a finestra fissa): ogni chiamata incrementa di 1 un contatore su Redis con INCR. INCR è un'operazione atomica — Redis la esegue tutta in un colpo solo, senza che due richieste arrivate nello stesso istante possano "pestarsi i piedi" (a differenza di un ipotetico "leggi il valore, poi scrivi valore+1" fatto a mano, dove due richieste concorrenti potrebbero leggere lo stesso valore ed entrambe pensare di essere la prima).

Al primo incremento (count == 1, cioè la chiave non esisteva prima) imposto anche una scadenza (KeyExpireAsync) pari alla finestra: dopo quel tempo Redis cancella da solo la chiave e il conteggio riparte da zero. Gli incrementi successivi nella stessa finestra non toccano più la scadenza — è per questo che si chiama "finestra fissa": conta da quando è partita la prima richiesta, non si allunga ad ogni nuova richiesta.

Nota: qui non uso ICacheService/RedisCacheService (quello della cache delle stanze) perché quello sa solo fare Get/Set/Remove su un blob JSON — non ha un'operazione di incremento atomico. Per questo RedisRateLimiter parla direttamente con IConnectionMultiplexer/IDatabase (le classi di più basso livello della libreria StackExchange.Redis), un accesso a Redis parallelo e indipendente da quello della cache.

4. Come si "aggancia" a una richiesta — RateLimitAttribute (API)

BookingSystem.API/Filters/RateLimitAttribute.cs
BookingSystem.API/Filters/RateLimitKeyType.cs
RateLimitKeyType è solo un enum con due valori: IpAddress o UserId — dice all'attributo da cosa costruire la chiave di rate limiting per quella specifica richiesta.

RateLimitAttribute implementa IAsyncActionFilter — un "filtro" che ASP.NET Core esegue automaticamente prima che il Controller elabori la richiesta (è lo stesso meccanismo, concettualmente, con cui [Authorize] blocca una richiesta prima che arrivi al Controller). Il metodo che avevi selezionato, IsAllowedAsync, è proprio il punto in cui questo filtro chiama IRateLimiter:

public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
{
var rateLimiter = context.HttpContext.RequestServices.GetRequiredService<IRateLimiter>();
var policies = context.HttpContext.RequestServices.GetRequiredService<IOptions<Dictionary<string, RateLimitPolicy>>>().Value;
var policy = policies[_policyName]; // es. "Login" -> Limit=5, WindowSeconds=60

    var identifier = _keyType switch
    {
        RateLimitKeyType.IpAddress => context.HttpContext.Connection.RemoteIpAddress?.ToString(),
        RateLimitKeyType.UserId    => context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
        ...
    };

    var key = $"ratelimit:{_policyName}:{identifier}";
    var allowed = await rateLimiter.IsAllowedAsync(key, policy.Limit, TimeSpan.FromSeconds(policy.WindowSeconds));

    if (!allowed)
    {
        context.Result = new ObjectResult(new { error = "..." }) { StatusCode = 429 };
        return;   // <- non chiama next(): il Controller non viene mai eseguito
    }

    await next();  // <- via libera, esegue il Controller normalmente

}
Si usa così, sopra un'azione del Controller (esattamente come [Authorize]):

[HttpPost("login")]
[RateLimit("Login", RateLimitKeyType.IpAddress)]
public async Task<IActionResult> Login(...) 5. Il collante — Program.cs

builder.Services.Configure<Dictionary<string, RateLimitPolicy>>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddSingleton<IConnectionMultiplexer>(
ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<IRateLimiter, RedisRateLimiter>();
Tre registrazioni nel contenitore di Dependency Injection:

La sezione RateLimiting di appsettings.json diventa un Dictionary<string, RateLimitPolicy> iniettabile.
Una connessione Redis dedicata (IConnectionMultiplexer) — diversa da quella usata dalla cache, perché AddStackExchangeRedisCache (registrato per la cache) gestisce la propria connessione internamente e non la espone per un uso diretto come questo.
IRateLimiter → quando qualcosa lo richiede, riceve un RedisRateLimiter.
Il flusso completo, per POST /api/auth/login

Richiesta HTTP
↓
RateLimitAttribute.OnActionExecutionAsync (intercetta PRIMA del Controller)
↓
legge la policy "Login" (5 richieste / 60s) da appsettings
↓
costruisce la chiave "ratelimit:login:{ip del chiamante}"
↓
IRateLimiter.IsAllowedAsync(key, 5, 60s)
↓
RedisRateLimiter → INCR sulla chiave su Redis
↓
├─ count <= 5 → true → next() → AuthController.Login viene eseguito normalmente
└─ count > 5 → false → risposta 429 immediata, AuthController.Login NON viene mai chiamato
Il Controller (AuthController) non contiene nessuna riga legata a Redis o al rate limiting — sa solo di avere l'attributo sopra il metodo, esattamente come non sa nulla della cache dietro IRoomService.
