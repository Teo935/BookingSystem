using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using BookingSystem.Application.Interfaces;
using BookingSystem.Application.Services;
using BookingSystem.Infrastructure.Caching;
using BookingSystem.Infrastructure.Data;
using BookingSystem.Infrastructure.Identity;
using BookingSystem.Infrastructure.Messaging;
using BookingSystem.Infrastructure.RateLimiting;
using BookingSystem.Infrastructure.Repositories;
using RabbitMQ.Client;
using StackExchange.Redis;

// Composition root del progetto: l'unico punto dove tutti e 4 i layer della Clean
// Architecture (Domain/Application/Infrastructure/API) vengono "collegati" tramite
// Dependency Injection (DI). Ogni interfaccia definita in Application viene qui
// abbinata alla sua implementazione concreta in Infrastructure — il resto del codice
// (Controller, Service) dipende sempre e solo dalle interfacce, mai da questo file.
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Inserire: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// EnableRetryOnFailure: in ambiente Docker Compose, "depends_on" garantisce solo
// l'ordine di AVVIO dei container, non che SQL Server sia già pronto ad accettare
// connessioni — il retry automatico rende l'app resiliente a questo scenario.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

// ASP.NET Core Identity: gestisce utenti, password (hashing), ruoli e le relative
// tabelle EF Core, appoggiandosi allo stesso AppDbContext usato per Room/Booking.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Missing 'Jwt' configuration section.");
builder.Services.AddSingleton(jwtSettings);
builder.Services.AddSingleton<JwtTokenGenerator>();

// JWT Bearer come schema di autenticazione di default: ogni richiesta viene
// autenticata leggendo e validando il token dall'header "Authorization: Bearer ...".
// I 4 "Validate*" insieme garantiscono che il token non sia scaduto, sia firmato con
// la chiave giusta, e sia stato emesso da/per questa applicazione (Issuer/Audience).
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings.Issuer,
        ValidAudience = jwtSettings.Audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
    };
});

builder.Services.AddAuthorization();

// Cache Redis per GET /api/rooms (vedi CachedRoomService più sotto): IDistributedCache
// è l'astrazione ASP.NET Core, RedisCacheService la implementa dietro ICacheService.
builder.Services.Configure<CacheSettings>(builder.Configuration.GetSection("Caching"));
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
});
builder.Services.AddSingleton<ICacheService, RedisCacheService>();

// Rate limiting: IConnectionMultiplexer registrato qui come singleton dedicato, separato
// da quello interno usato da AddStackExchangeRedisCache (che non lo espone), perché
// RedisRateLimiter/RedisRefreshTokenStore hanno bisogno di comandi Redis diretti
// (INCR, EXPIRE, DELETE) non disponibili tramite IDistributedCache.
builder.Services.Configure<Dictionary<string, RateLimitPolicy>>(builder.Configuration.GetSection("RateLimiting"));
builder.Services.AddSingleton<IConnectionMultiplexer>(
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));
builder.Services.AddSingleton<IRateLimiter, RedisRateLimiter>();

builder.Services.AddSingleton<IRefreshTokenStore, RedisRefreshTokenStore>();

// RabbitMQ: connessione singleton condivisa da publisher (RabbitMqEventPublisher) e
// consumer (BookingNotificationConsumer, registrato come Hosted Service — gira in
// sottofondo per tutta la vita dell'app).
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

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IRoomRepository, RoomRepository>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();

// Decorator Pattern in azione: RoomService (la business logic vera) è registrato con
// il suo tipo concreto, poi IRoomService viene fatto risolvere a CachedRoomService,
// che riceve RoomService come dipendenza "_inner". Chi chiede IRoomService (es.
// RoomsController) riceve quindi sempre CachedRoomService, senza saperlo.
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<IRoomService>(sp => new CachedRoomService(
    sp.GetRequiredService<RoomService>(),
    sp.GetRequiredService<ICacheService>(),
    sp.GetRequiredService<IOptions<CacheSettings>>()));

var app = builder.Build();

// Ad ogni avvio: applica automaticamente le Migrations EF Core pendenti (utile in
// Docker, dove il container SQL Server può partire vuoto) e poi crea i ruoli
// Admin/User e l'utente Admin iniziale se non esistono ancora.
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var adminSeed = builder.Configuration.GetSection("AdminSeed").Get<AdminSeedOptions>() ?? new AdminSeedOptions();
    await IdentitySeeder.SeedAsync(roleManager, userManager, adminSeed);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// L'ordine della pipeline di middleware è significativo: l'autenticazione (chi sei,
// letto dal JWT) deve avvenire prima dell'autorizzazione (cosa puoi fare, in base a
// ruoli/[Authorize]) — invertire questi due causerebbe errori di autorizzazione anche
// per richieste con un token valido.
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
