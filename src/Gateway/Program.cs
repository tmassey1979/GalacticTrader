using System.Threading.RateLimiting;
using GalacticTrader.Gateway;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Yarp.ReverseProxy.Configuration;

var builder = WebApplication.CreateBuilder(args);
var gatewayOptions = GatewayRuntimeOptions.FromConfiguration(builder.Configuration);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = gatewayOptions.JwtAuthority;
        options.Audience = gatewayOptions.JwtAudience;
        options.RequireHttpsMetadata = gatewayOptions.RequireHttpsMetadata;
    });

builder.Services
    .AddAuthorizationBuilder()
    .AddPolicy("RequireJwt", policy => policy.RequireAuthenticatedUser());

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
        var forwardedKey = string.IsNullOrWhiteSpace(forwardedFor)
            ? null
            : forwardedFor.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var partitionKey = forwardedKey
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: partitionKey,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = gatewayOptions.PermitLimit,
                Window = TimeSpan.FromSeconds(gatewayOptions.WindowSeconds),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

var routes = new[]
{
    new RouteConfig
    {
        RouteId = "auth-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch { Path = "/api/auth/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "swagger-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch { Path = "/swagger/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "openapi-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch { Path = "/openapi/{**catch-all}" }
    },
    new RouteConfig
    {
        RouteId = "metrics-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch { Path = "/metrics" }
    },
    new RouteConfig
    {
        RouteId = "api-route",
        ClusterId = "api-cluster",
        Match = new RouteMatch { Path = "/api/{**catch-all}" },
        AuthorizationPolicy = "RequireJwt"
    }
};

var clusters = new[]
{
    new ClusterConfig
    {
        ClusterId = "api-cluster",
        Destinations = new Dictionary<string, DestinationConfig>
        {
            ["primary"] = new DestinationConfig
            {
                Address = $"{gatewayOptions.ApiBaseUrl}/"
            }
        }
    }
};

builder.Services
    .AddReverseProxy()
    .LoadFromMemory(routes, clusters);

var app = builder.Build();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "ok" }));
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready" }));

app.MapReverseProxy();
app.Run();

public partial class Program;
