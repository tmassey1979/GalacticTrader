namespace GalacticTrader.Services.Authentication;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Middleware for JWT token validation and user principal extraction
/// </summary>
public class JwtValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtValidationMiddleware> _logger;

    public JwtValidationMiddleware(RequestDelegate next, ILogger<JwtValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, ITokenValidationService tokenValidator)
    {
        // Skip validation for public endpoints
        if (IsPublicEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        try
        {
            var token = ExtractToken(context.Request);
            
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("No JWT token found in request");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Missing authorization token" });
                return;
            }

            var principal = await tokenValidator.ValidateTokenAsync(token);
            
            if (principal == null)
            {
                _logger.LogWarning("JWT token validation failed");
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new { error = "Invalid token" });
                return;
            }

            // Attach principal to HttpContext for downstream handlers
            context.User = principal;
            
            _logger.LogInformation("JWT token validated for user: {UserId}", principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during JWT validation");
            context.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await context.Response.WriteAsJsonAsync(new { error = "Authentication error" });
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Extract JWT token from Authorization header
    /// </summary>
    private static string? ExtractToken(HttpRequest request)
    {
        const string bearerScheme = "Bearer ";
        
        var authHeader = request.Headers.Authorization.ToString();
        
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith(bearerScheme, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return authHeader.Substring(bearerScheme.Length);
    }

    /// <summary>
    /// Check if endpoint is public and doesn't require authentication
    /// </summary>
    private static bool IsPublicEndpoint(PathString path)
    {
        var pathValue = path.Value ?? string.Empty;

        // Public endpoints that don't require authentication
        return pathValue.EndsWith("/health", StringComparison.OrdinalIgnoreCase)
            || pathValue.EndsWith("/health/live", StringComparison.OrdinalIgnoreCase)
            || pathValue.EndsWith("/health/ready", StringComparison.OrdinalIgnoreCase)
            || pathValue.Contains("/swagger", StringComparison.OrdinalIgnoreCase)
            || pathValue.Contains("/auth/login", StringComparison.OrdinalIgnoreCase)
            || pathValue.Contains("/auth/callback", StringComparison.OrdinalIgnoreCase)
            || pathValue.Contains("/.well-known", StringComparison.OrdinalIgnoreCase);
    }
}
