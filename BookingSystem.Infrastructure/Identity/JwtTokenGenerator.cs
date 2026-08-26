using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace BookingSystem.Infrastructure.Identity;

public class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings)
    {
        _settings = settings;
    }

    // Genera l'Access Token JWT (JSON Web Token): un token auto-contenuto, firmato con
    // HMAC-SHA256 usando una chiave simmetrica condivisa (SecretKey). I claim (UserId,
    // Email, Roles) sono leggibili da chiunque decodifichi il token (JWT non è
    // criptato, solo firmato) ma non modificabili senza invalidare la firma — per
    // questo il middleware di autenticazione può fidarsi dei ruoli contenuti senza
    // dover interrogare il database ad ogni richiesta.
    public (string Token, DateTime ExpiresAt) GenerateToken(ApplicationUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Email, user.Email ?? string.Empty)
        };
        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expiresAt = DateTime.UtcNow.AddMinutes(_settings.ExpirationMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return (new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    // Il refresh token, a differenza dell'Access Token, NON è un JWT: è solo una
    // stringa casuale opaca (64 byte da un generatore crittograficamente sicuro,
    // codificati Base64Url). Non contiene alcuna informazione decodificabile — va
    // confrontato con quanto salvato in Redis (RedisRefreshTokenStore). Il vantaggio è
    // che può essere revocato lato server in ogni momento, cosa che un JWT firmato non
    // permetterebbe senza mantenere una blocklist.
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    public TimeSpan RefreshTokenExpiration => TimeSpan.FromDays(_settings.RefreshTokenExpirationDays);
}
