namespace GalacticTrader.Gateway;

public sealed class GatewayRuntimeOptions
{
    public required string ApiBaseUrl { get; init; }
    public required string JwtAuthority { get; init; }
    public required string JwtAudience { get; init; }
    public required bool RequireHttpsMetadata { get; init; }
    public required int PermitLimit { get; init; }
    public required int WindowSeconds { get; init; }

    public static GatewayRuntimeOptions FromConfiguration(IConfiguration configuration)
    {
        var apiBaseUrl = configuration["Gateway:ApiBaseUrl"];
        if (string.IsNullOrWhiteSpace(apiBaseUrl))
        {
            apiBaseUrl = "http://api:8080";
        }

        var jwtAuthority = configuration["Gateway:Jwt:Authority"];
        if (string.IsNullOrWhiteSpace(jwtAuthority))
        {
            jwtAuthority = "http://keycloak:8080/realms/galactictrader";
        }

        var jwtAudience = configuration["Gateway:Jwt:Audience"];
        if (string.IsNullOrWhiteSpace(jwtAudience))
        {
            jwtAudience = "account";
        }

        return new GatewayRuntimeOptions
        {
            ApiBaseUrl = apiBaseUrl.TrimEnd('/'),
            JwtAuthority = jwtAuthority.TrimEnd('/'),
            JwtAudience = jwtAudience,
            RequireHttpsMetadata = bool.TryParse(configuration["Gateway:Jwt:RequireHttpsMetadata"], out var requireHttps)
                ? requireHttps
                : false,
            PermitLimit = int.TryParse(configuration["Gateway:RateLimit:PermitLimit"], out var permitLimit)
                ? Math.Clamp(permitLimit, 10, 10_000)
                : 300,
            WindowSeconds = int.TryParse(configuration["Gateway:RateLimit:WindowSeconds"], out var windowSeconds)
                ? Math.Clamp(windowSeconds, 1, 3600)
                : 60
        };
    }
}
