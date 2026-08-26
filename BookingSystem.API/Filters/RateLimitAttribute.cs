using System.Security.Claims;
using BookingSystem.Application.Interfaces;
using BookingSystem.Infrastructure.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace BookingSystem.API.Filters;

// Action Filter applicato come attributo dichiarativo (es. [RateLimit("Login",
// RateLimitKeyType.IpAddress)]) sopra l'action del Controller: nessuna logica Redis
// dentro ai Controller stessi. IAsyncActionFilter intercetta la richiesta PRIMA che
// l'azione venga eseguita — se il limite è superato, imposta context.Result e non
// chiama mai next(), quindi l'azione del Controller non viene eseguita affatto.
[AttributeUsage(AttributeTargets.Method)]
public class RateLimitAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _policyName;
    private readonly RateLimitKeyType _keyType;

    public RateLimitAttribute(string policyName, RateLimitKeyType keyType)
    {
        _policyName = policyName;
        _keyType = keyType;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var rateLimiter = context.HttpContext.RequestServices.GetRequiredService<IRateLimiter>();
        var policies = context.HttpContext.RequestServices.GetRequiredService<IOptions<Dictionary<string, RateLimitPolicy>>>().Value;

        if (!policies.TryGetValue(_policyName, out var policy))
        {
            throw new InvalidOperationException($"Missing rate limit policy '{_policyName}' in configuration section 'RateLimiting'.");
        }

        var identifier = _keyType switch
        {
            RateLimitKeyType.IpAddress => context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            RateLimitKeyType.UserId => context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown",
            _ => "unknown"
        };

        var key = $"ratelimit:{_policyName.ToLowerInvariant()}:{identifier}";
        var allowed = await rateLimiter.IsAllowedAsync(key, policy.Limit, TimeSpan.FromSeconds(policy.WindowSeconds));

        if (!allowed)
        {
            context.Result = new ObjectResult(new { error = "Too many requests. Please try again later." })
            {
                StatusCode = StatusCodes.Status429TooManyRequests
            };
            return;
        }

        await next();
    }
}
