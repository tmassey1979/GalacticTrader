using GalacticTrader.API.Telemetry;
using GalacticTrader.API.Swagger;
using GalacticTrader.API.Secrets;
using GalacticTrader.API.Contracts;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
using GalacticTrader.Services.Admin;
using GalacticTrader.Services.Authentication;
using GalacticTrader.Services.Communication;
using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Economy;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Auth;
using GalacticTrader.Services.Leaderboard;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Npc;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;
using GalacticTrader.Services.Realtime;
using GalacticTrader.Services.Telemetry;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Prometheus;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Cryptography;
using System.Security.Claims;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddVaultSecretsIfConfigured();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Galactic Trader API",
        Version = "v1",
        Description = "Server-authoritative simulation API for navigation, trading, combat, fleet, reputation, and communication."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Paste the bearer token in this format: Bearer {token}",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    });

    options.OperationFilter<DefaultErrorResponsesOperationFilter>();
    options.SchemaFilter<SwaggerExampleSchemaFilter>();
});
builder.Services.AddOpenApi();

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? builder.Configuration["ConnectionStrings:Default"];

if (string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<GalacticTraderDbContext>(options =>
        options.UseInMemoryDatabase("GalacticTraderDev"));
}
else
{
    builder.Services.AddDbContext<GalacticTraderDbContext>(options =>
        options.UseNpgsql(connectionString));
}

builder.Services.AddSingleton<ICacheService, InMemoryCacheService>();
builder.Services.AddScoped<ISectorRepository, SectorRepository>();
builder.Services.AddScoped<IRouteRepository, RouteRepository>();
builder.Services.AddScoped<IGraphValidationService, GraphValidationService>();
builder.Services.AddScoped<ISectorService, SectorService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IRoutePlanningService, RoutePlanningService>();
builder.Services.AddScoped<IAutopilotService, AutopilotService>();
builder.Services.AddScoped<ICombatService, CombatService>();
builder.Services.AddScoped<IEconomyService, EconomyService>();
builder.Services.AddScoped<IMarketTransactionService, MarketTransactionService>();
builder.Services.AddScoped<INpcService, NpcService>();
builder.Services.AddScoped<IFleetService, FleetService>();
builder.Services.AddScoped<IReputationService, ReputationService>();
builder.Services.AddScoped<ILeaderboardService, LeaderboardService>();
builder.Services.AddScoped<ICommunicationService, CommunicationService>();
builder.Services.AddScoped<IStrategicSystemsService, StrategicSystemsService>();
builder.Services.AddScoped<IDashboardRealtimeSnapshotService, DashboardRealtimeSnapshotService>();
builder.Services.AddScoped<IGlobalMetricsService, GlobalMetricsService>();
builder.Services.AddScoped<IMarketIntelligenceService, MarketIntelligenceService>();
builder.Services.AddSingleton<IBalanceControlService, BalanceControlService>();
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddOptions<KeycloakOptions>()
    .Bind(builder.Configuration.GetSection(KeycloakOptions.SectionName));
builder.Services.AddHttpClient<ITokenValidationService, KeycloakTokenValidationService>();
builder.Services.AddSingleton<IVoiceService, VoiceService>();
builder.Services.AddHostedService<TelemetryGaugeRefreshService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();
    await dbContext.Database.EnsureCreatedAsync();

    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
    await EnsureBootstrapAdminPlayerAsync(dbContext, authService, builder.Configuration, CancellationToken.None);
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseWebSockets();
var channelSockets = new ConcurrentDictionary<Guid, (WebSocket Socket, ChannelType ChannelType, string ChannelKey)>();

app.Use(async (context, next) =>
{
    var stopwatch = Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    var route = context.GetEndpoint()?.DisplayName ?? context.Request.Path.Value ?? "unknown";
    var durationSeconds = stopwatch.Elapsed.TotalSeconds;
    var statusCode = context.Response.StatusCode.ToString();

    PrometheusMetrics.ApiRequestDuration
        .WithLabels(context.Request.Method, route, statusCode)
        .Observe(durationSeconds);

    // Captures high-level DB-bound request time for observability.
    PrometheusMetrics.DbQueryDuration.Observe(durationSeconds);
});

app.MapMetrics("/metrics");

var telemetry = app.MapGroup("/api/telemetry")
    .WithTags("Telemetry");

telemetry.MapGet("/global-summary", async (
    IGlobalMetricsService globalMetricsService,
    CancellationToken cancellationToken) =>
{
    var summary = await globalMetricsService.GetGlobalSummaryAsync(cancellationToken);
    return Results.Ok(summary);
});

telemetry.MapGet("/market-intelligence", async (
    int? limit,
    IMarketIntelligenceService marketIntelligenceService,
    CancellationToken cancellationToken) =>
{
    var summary = await marketIntelligenceService.GetSummaryAsync(limit ?? 8, cancellationToken);
    return Results.Ok(summary);
});

var auth = app.MapGroup("/api/auth")
    .WithTags("Authentication");

auth.MapPost("/register", async (
    RegisterPlayerApiRequest request,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) ||
        request.Username.Trim().Length < 3)
    {
        return Results.BadRequest(new { error = "Username must be at least 3 characters long." });
    }

    if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
    {
        return Results.BadRequest(new { error = "A valid email address is required." });
    }

    if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
    {
        return Results.BadRequest(new { error = "Password must be at least 8 characters long." });
    }

    if (request.Birthdate is DateOnly birthdate &&
        birthdate > DateOnly.FromDateTime(DateTime.UtcNow))
    {
        return Results.BadRequest(new { error = "Birthdate cannot be in the future." });
    }

    if (!string.IsNullOrWhiteSpace(request.Website) &&
        !Uri.TryCreate(request.Website, UriKind.Absolute, out _))
    {
        return Results.BadRequest(new { error = "Website must be a valid absolute URL." });
    }

    try
    {
        var created = await authService.RegisterAsync(
            new RegisterPlayerRequest(
                request.Username,
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName,
                request.MiddleName,
                request.Nickname,
                request.Birthdate,
                request.Gender,
                request.Pronouns,
                request.PhoneNumber,
                request.Locale,
                request.TimeZone,
                request.Website),
            cancellationToken);

        await EnsureUserAccountRolesAsync(
            dbContext,
            created.PlayerId,
            created.Username,
            created.Email,
            [AuthorizationPolicies.PlayerRole],
            cancellationToken);
        await BootstrapNewPlayerAsync(dbContext, created, cancellationToken);
        return Results.Created($"/api/auth/players/{created.PlayerId}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Register a player account";
        operation.Description = "Creates a player identity, stores credentials, and provisions starter gameplay assets.";
        return operation;
    });

auth.MapPost("/login", async (
    LoginPlayerApiRequest request,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    IOptions<KeycloakOptions> keycloakOptionsAccessor,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
    }

    var keycloakOptions = keycloakOptionsAccessor.Value;
    var keycloakAttempt = await TryLoginAgainstKeycloakAsync(
        request.Username,
        request.Password,
        keycloakOptions,
        httpClientFactory,
        cancellationToken);

    if (keycloakAttempt.Success &&
        keycloakAttempt.Session is { } keycloakSession)
    {
        var normalizedRoles = keycloakSession.Roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (!normalizedRoles.Contains(AuthorizationPolicies.PlayerRole, StringComparer.OrdinalIgnoreCase))
        {
            normalizedRoles.Add(AuthorizationPolicies.PlayerRole);
        }

        var identity = await EnsurePlayerIdentityForKeycloakLoginAsync(
            dbContext,
            keycloakSession.KeycloakUserId,
            keycloakSession.KeycloakUserIdAsGuid,
            keycloakSession.Username,
            keycloakSession.Email,
            normalizedRoles,
            cancellationToken);

        await BootstrapNewPlayerAsync(
            dbContext,
            identity,
            cancellationToken,
            keycloakSession.KeycloakUserIdAsGuid);

        return Results.Ok(new LoginResult(identity, keycloakSession.AccessToken, keycloakSession.ExpiresAtUtc));
    }

    if (keycloakAttempt.InvalidCredentials)
    {
        return Results.Unauthorized();
    }

    var loginResult = await authService.LoginAsync(
        new LoginPlayerRequest(request.Username, request.Password),
        cancellationToken);

    return loginResult is null
        ? Results.Unauthorized()
        : Results.Ok(loginResult);
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Authenticate and get bearer token";
        operation.Description = "Validates credentials and returns a temporary bearer token suitable for API calls.";
        return operation;
    });

auth.MapGet("/validate", async (
    string token,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    var session = await authService.ValidateTokenAsync(token, cancellationToken);
    return session is null ? Results.Unauthorized() : Results.Ok(session);
})
    .WithOpenApi(operation =>
    {
        operation.Summary = "Validate bearer token";
        operation.Description = "Returns the active session when a bearer token is still valid.";
        return operation;
    });

var sectors = app.MapGroup("/api/navigation/sectors")
    .WithTags("Navigation - Sectors");

sectors.MapGet("/", async (ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAllSectorsAsync(cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/{sectorId:guid}", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
    var sector = await service.GetSectorByIdAsync(sectorId, cancellationToken);
    return sector is null ? Results.NotFound() : Results.Ok(sector);
});

sectors.MapGet("/{sectorId:guid}/adjacent", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAdjacentSectorsAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/high-security", async (int? threshold, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetHighSecuritySectorsAsync(threshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

sectors.MapGet("/high-risk", async (int? threshold, ISectorService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetHighRiskSectorsAsync(threshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

sectors.MapPost("/", async (
    HttpContext context,
    CreateSectorRequest request,
    ISectorService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    try
    {
        var created = await service.CreateSectorAsync(request.Name, request.X, request.Y, request.Z, cancellationToken);
        return Results.Created($"/api/navigation/sectors/{created.Id}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

sectors.MapPut("/{sectorId:guid}", async (
    HttpContext context,
    Guid sectorId,
    UpdateSectorRequest request,
    ISectorService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var updated = await service.UpdateSectorAsync(
        sectorId,
        request.SecurityLevel,
        request.HazardRating,
        request.FactionId,
        cancellationToken);

    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

sectors.MapDelete("/{sectorId:guid}", async (
    HttpContext context,
    Guid sectorId,
    ISectorService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var deleted = await service.DeleteSectorAsync(sectorId, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

var routes = app.MapGroup("/api/navigation/routes")
    .WithTags("Navigation - Routes");

routes.MapGet("/", async (IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetAllRoutesAsync(cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/{routeId:guid}", async (Guid routeId, IRouteService service, CancellationToken cancellationToken) =>
{
    var route = await service.GetRouteByIdAsync(routeId, cancellationToken);
    return route is null ? Results.NotFound() : Results.Ok(route);
});

routes.MapGet("/outbound/{sectorId:guid}", async (Guid sectorId, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetOutboundRoutesAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/inbound/{sectorId:guid}", async (Guid sectorId, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetInboundRoutesAsync(sectorId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/between/{sectorAId:guid}/{sectorBId:guid}", async (
    Guid sectorAId,
    Guid sectorBId,
    IRouteService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetRoutesBetweenAsync(sectorAId, sectorBId, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/dangerous", async (int? riskThreshold, IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetDangerousRoutesAsync(riskThreshold ?? 70, cancellationToken);
    return Results.Ok(result);
});

routes.MapGet("/legal", async (IRouteService service, CancellationToken cancellationToken) =>
{
    var result = await service.GetLegalRoutesAsync(cancellationToken);
    return Results.Ok(result);
});

routes.MapPost("/", async (
    HttpContext context,
    CreateRouteRequest request,
    IRouteService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    try
    {
        var created = await service.CreateRouteAsync(
            request.FromSectorId,
            request.ToSectorId,
            request.LegalStatus,
            request.WarpGateType,
            cancellationToken);
        return Results.Created($"/api/navigation/routes/{created.Id}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.Conflict(new { error = exception.Message });
    }
});

routes.MapPut("/{routeId:guid}", async (
    HttpContext context,
    Guid routeId,
    UpdateRouteRequest request,
    IRouteService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var updated = await service.UpdateRouteAsync(routeId, request.LegalStatus, request.BaseRiskScore, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

routes.MapDelete("/{routeId:guid}", async (
    HttpContext context,
    Guid routeId,
    IRouteService service,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireMapAdminAsync(context, authService, dbContext, cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var deleted = await service.DeleteRouteAsync(routeId, cancellationToken);
    return deleted ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/navigation/graph/validate", async (IGraphValidationService validationService, CancellationToken cancellationToken) =>
{
    var report = await validationService.ValidateGraphAsync(cancellationToken);
    return Results.Ok(report);
})
    .WithTags("Navigation - Graph");

var planning = app.MapGroup("/api/navigation/planning")
    .WithTags("Navigation - Planning");

planning.MapGet("/{fromSectorId:guid}/{toSectorId:guid}", async (
    Guid fromSectorId,
    Guid toSectorId,
    TravelMode? mode,
    string? algorithm,
    IRoutePlanningService planningService,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var plan = await planningService.CalculateRouteAsync(
        fromSectorId,
        toSectorId,
        mode ?? TravelMode.Standard,
        string.IsNullOrWhiteSpace(algorithm) ? "dijkstra" : algorithm,
        cancellationToken);
    stopwatch.Stop();
    PrometheusMetrics.RouteCalculationDuration.Observe(stopwatch.Elapsed.TotalSeconds);

    return plan is null
        ? Results.NotFound(new { error = "No route found between the selected sectors." })
        : Results.Ok(plan);
});

planning.MapGet("/{fromSectorId:guid}/{toSectorId:guid}/optimize", async (
    Guid fromSectorId,
    Guid toSectorId,
    IRoutePlanningService planningService,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var optimization = await planningService.GetOptimizedRoutesAsync(fromSectorId, toSectorId, cancellationToken);
    stopwatch.Stop();
    PrometheusMetrics.RouteCalculationDuration.Observe(stopwatch.Elapsed.TotalSeconds);
    return Results.Ok(optimization);
});

var autopilot = app.MapGroup("/api/navigation/autopilot")
    .WithTags("Navigation - Autopilot");

autopilot.MapPost("/start", async (
    StartAutopilotRequest request,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var session = await autopilotService.StartAutopilotAsync(request, cancellationToken);
        return Results.Created($"/api/navigation/autopilot/{session.SessionId}", session);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

autopilot.MapGet("/{sessionId:guid}", async (
    Guid sessionId,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    var session = await autopilotService.GetSessionAsync(sessionId, cancellationToken);
    return session is null ? Results.NotFound() : Results.Ok(session);
});

autopilot.MapGet("/active", async (IAutopilotService autopilotService, CancellationToken cancellationToken) =>
{
    var sessions = await autopilotService.GetActiveSessionsAsync(cancellationToken);
    return Results.Ok(sessions);
});

autopilot.MapPost("/{sessionId:guid}/tick", async (
    Guid sessionId,
    int? seconds,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    var result = await autopilotService.ProcessTickAsync(sessionId, seconds ?? 1, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

autopilot.MapPost("/tick-active", async (
    int? seconds,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    var results = await autopilotService.ProcessActiveTicksAsync(seconds ?? 1, cancellationToken);
    return Results.Ok(results);
});

autopilot.MapPost("/{sessionId:guid}/transition", async (
    Guid sessionId,
    TransitionTravelModeRequest request,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var session = await autopilotService.TransitionTravelModeAsync(
            sessionId,
            request.TargetMode,
            request.Reason,
            cancellationToken);
        return session is null ? Results.NotFound() : Results.Ok(session);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

autopilot.MapPost("/{sessionId:guid}/cancel", async (
    Guid sessionId,
    IAutopilotService autopilotService,
    CancellationToken cancellationToken) =>
{
    var cancelled = await autopilotService.CancelAsync(sessionId, cancellationToken);
    return cancelled ? Results.NoContent() : Results.NotFound();
});

var combat = app.MapGroup("/api/combat")
    .WithTags("Combat");

combat.MapPost("/start", async (
    StartCombatRequest request,
    ICombatService combatService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var started = await combatService.StartCombatAsync(request, cancellationToken);
        return Results.Created($"/api/combat/{started.CombatId}", started);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

combat.MapGet("/active", async (ICombatService combatService, CancellationToken cancellationToken) =>
{
    var active = await combatService.GetActiveCombatsAsync(cancellationToken);
    return Results.Ok(active);
});

combat.MapGet("/logs", async (int? limit, ICombatService combatService, CancellationToken cancellationToken) =>
{
    var logs = await combatService.GetRecentCombatLogsAsync(limit ?? 50, cancellationToken);
    return Results.Ok(logs);
});

combat.MapGet("/{combatId:guid}", async (
    Guid combatId,
    ICombatService combatService,
    CancellationToken cancellationToken) =>
{
    var state = await combatService.GetCombatAsync(combatId, cancellationToken);
    return state is null ? Results.NotFound() : Results.Ok(state);
});

combat.MapPost("/{combatId:guid}/tick", async (
    Guid combatId,
    ICombatService combatService,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var tick = await combatService.ProcessTickAsync(combatId, cancellationToken);
    stopwatch.Stop();
    PrometheusMetrics.CombatTickDuration.Observe(stopwatch.Elapsed.TotalSeconds);
    return tick is null ? Results.NotFound() : Results.Ok(tick);
});

combat.MapPost("/{combatId:guid}/ticks", async (
    Guid combatId,
    int? count,
    ICombatService combatService,
    CancellationToken cancellationToken) =>
{
    var stopwatch = Stopwatch.StartNew();
    var results = await combatService.ProcessTicksAsync(combatId, count ?? 1, cancellationToken);
    stopwatch.Stop();
    PrometheusMetrics.CombatTickDuration.Observe(stopwatch.Elapsed.TotalSeconds);
    return Results.Ok(results);
});

combat.MapPost("/{combatId:guid}/end", async (
    Guid combatId,
    ICombatService combatService,
    CancellationToken cancellationToken) =>
{
    var result = await combatService.EndCombatAsync(combatId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

var economy = app.MapGroup("/api/economy")
    .WithTags("Economy");

economy.MapGet("/commodities/hierarchy", async (
    IEconomyService economyService,
    CancellationToken cancellationToken) =>
{
    var hierarchy = await economyService.GetCommodityHierarchyAsync(cancellationToken);
    return Results.Ok(hierarchy);
});

economy.MapPost("/tick", async (
    HttpContext context,
    IEconomyService economyService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var tick = await economyService.ProcessMarketTickAsync(cancellationToken);
    return Results.Ok(tick);
});

economy.MapPost("/market-shock", async (
    HttpContext context,
    MarketShockRequest request,
    IEconomyService economyService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var triggered = await economyService.TriggerMarketShockAsync(request, cancellationToken);
    return triggered ? Results.Accepted() : Results.BadRequest();
});

economy.MapPost("/price-preview", async (
    PriceCalculationInput input,
    IEconomyService economyService,
    CancellationToken cancellationToken) =>
{
    var result = await economyService.CalculatePriceAsync(input, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

var market = app.MapGroup("/api/market")
    .WithTags("Market");

market.MapPost("/trade", async (
    ExecuteTradeRequest request,
    IMarketTransactionService tradeService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await tradeService.ExecuteTradeAsync(request, cancellationToken);
        return Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

market.MapPost("/trade/reverse", async (
    ReverseTradeRequest request,
    IMarketTransactionService tradeService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await tradeService.ReverseTradeAsync(request, cancellationToken);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

market.MapGet("/transactions/{playerId:guid}", async (
    Guid playerId,
    int? limit,
    IMarketTransactionService tradeService,
    CancellationToken cancellationToken) =>
{
    var result = await tradeService.GetPlayerTransactionsAsync(playerId, limit ?? 50, cancellationToken);
    return Results.Ok(result);
});

var npc = app.MapGroup("/api/npc")
    .WithTags("NPC");

npc.MapPost("/agents", async (
    HttpContext context,
    CreateNpcRequest request,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    try
    {
        var created = await npcService.CreateAgentAsync(request, cancellationToken);
        return Results.Created($"/api/npc/agents/{created.Id}", created);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

npc.MapGet("/agents", async (INpcService npcService, CancellationToken cancellationToken) =>
{
    var agents = await npcService.GetAgentsAsync(cancellationToken);
    return Results.Ok(agents);
});

npc.MapGet("/agents/{agentId:guid}", async (
    Guid agentId,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
    var agent = await npcService.GetAgentAsync(agentId, cancellationToken);
    return agent is null ? Results.NotFound() : Results.Ok(agent);
});

npc.MapPost("/agents/{agentId:guid}/tick", async (
    HttpContext context,
    Guid agentId,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var result = await npcService.ProcessDecisionTickAsync(agentId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

npc.MapPost("/tick-all", async (
    HttpContext context,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var result = await npcService.ProcessAllDecisionTicksAsync(cancellationToken);
    return Results.Ok(result);
});

npc.MapPost("/agents/{agentId:guid}/fleet/spawn", async (
    HttpContext context,
    Guid agentId,
    int? ships,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var summary = await npcService.SpawnFleetAsync(agentId, ships ?? 3, cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

npc.MapPost("/agents/{agentId:guid}/route/{targetSectorId:guid}", async (
    HttpContext context,
    Guid agentId,
    Guid targetSectorId,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var planned = await npcService.PlanRouteAsync(agentId, targetSectorId, cancellationToken);
    return planned ? Results.Accepted() : Results.BadRequest(new { error = "Route planning failed." });
});

npc.MapPost("/agents/{agentId:guid}/move", async (
    HttpContext context,
    Guid agentId,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var moved = await npcService.ProcessFleetMovementAsync(agentId, cancellationToken);
    return moved ? Results.Accepted() : Results.BadRequest(new { error = "Movement processing failed." });
});

npc.MapPost("/agents/{agentId:guid}/trade", async (
    HttpContext context,
    Guid agentId,
    INpcService npcService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var margin = await npcService.ExecuteNpcTradeAsync(agentId, cancellationToken);
    return margin.HasValue ? Results.Ok(new { margin = margin.Value }) : Results.NotFound();
});

var fleet = app.MapGroup("/api/fleet")
    .WithTags("Fleet");

fleet.MapGet("/templates", async (IFleetService fleetService, CancellationToken cancellationToken) =>
{
    var templates = await fleetService.GetShipTemplatesAsync(cancellationToken);
    return Results.Ok(templates);
});

fleet.MapPost("/ships/purchase", async (
    PurchaseShipRequest request,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var ship = await fleetService.PurchaseShipAsync(request, cancellationToken);
        return ship is null ? Results.NotFound() : Results.Created($"/api/fleet/ships/{ship.Id}", ship);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

fleet.MapGet("/players/{playerId:guid}/ships", async (
    Guid playerId,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var ships = await fleetService.GetPlayerShipsAsync(playerId, cancellationToken);
    return Results.Ok(ships);
});

fleet.MapGet("/ships/{shipId:guid}", async (
    Guid shipId,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var ship = await fleetService.GetShipAsync(shipId, cancellationToken);
    return ship is null ? Results.NotFound() : Results.Ok(ship);
});

fleet.MapPost("/ships/modules", async (
    InstallShipModuleRequest request,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var ship = await fleetService.InstallModuleAsync(request, cancellationToken);
        return ship is null ? Results.NotFound() : Results.Ok(ship);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

fleet.MapPost("/crew/hire", async (
    HireCrewRequest request,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var crew = await fleetService.HireCrewAsync(request, cancellationToken);
        return crew is null ? Results.NotFound() : Results.Created($"/api/fleet/crew/{crew.Id}", crew);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

fleet.MapPost("/crew/{crewId:guid}/progress", async (
    Guid crewId,
    CrewProgressRequest request,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var crew = await fleetService.ProgressCrewAsync(crewId, request, cancellationToken);
    return crew is null ? Results.NotFound() : Results.Ok(crew);
});

fleet.MapDelete("/crew/{crewId:guid}", async (
    Guid crewId,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var fired = await fleetService.FireCrewAsync(crewId, cancellationToken);
    return fired ? Results.NoContent() : Results.NotFound();
});

fleet.MapGet("/players/{playerId:guid}/escort", async (
    Guid playerId,
    FleetFormation? formation,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var summary = await fleetService.GetEscortSummaryAsync(playerId, formation ?? FleetFormation.Defensive, cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

fleet.MapPost("/convoy/simulate", async (
    ConvoySimulationRequest request,
    IFleetService fleetService,
    CancellationToken cancellationToken) =>
{
    var simulation = await fleetService.SimulateConvoyAsync(request, cancellationToken);
    return simulation is null ? Results.NotFound() : Results.Ok(simulation);
});

var reputation = app.MapGroup("/api/reputation")
    .WithTags("Reputation");

reputation.MapPost("/factions/adjust", async (
    UpdateFactionStandingRequest request,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var standing = await reputationService.AdjustFactionStandingAsync(request, cancellationToken);
    return standing is null ? Results.NotFound() : Results.Ok(standing);
});

reputation.MapGet("/factions/{playerId:guid}", async (
    Guid playerId,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var standings = await reputationService.GetFactionStandingsAsync(playerId, cancellationToken);
    return Results.Ok(standings);
});

reputation.MapPost("/factions/decay", async (
    int? points,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var updated = await reputationService.ApplyFactionReputationDecayAsync(points ?? 1, cancellationToken);
    return Results.Ok(new { updated });
});

reputation.MapGet("/factions/{playerId:guid}/benefits", async (
    Guid playerId,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var benefits = await reputationService.GetFactionBenefitsAsync(playerId, cancellationToken);
    return Results.Ok(benefits);
});

reputation.MapPost("/alignment/action", async (
    AlignmentActionRequest request,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var alignment = await reputationService.ApplyAlignmentActionAsync(request, cancellationToken);
    return alignment is null ? Results.NotFound() : Results.Ok(alignment);
});

reputation.MapGet("/alignment/{playerId:guid}", async (
    Guid playerId,
    IReputationService reputationService,
    CancellationToken cancellationToken) =>
{
    var access = await reputationService.GetAlignmentAccessAsync(playerId, cancellationToken);
    return access is null ? Results.NotFound() : Results.Ok(access);
});

var leaderboards = app.MapGroup("/api/leaderboards")
    .WithTags("Leaderboards");

leaderboards.MapPost("/recalculate", async (
    HttpContext context,
    ILeaderboardService leaderboardService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    var recalculated = await leaderboardService.RecalculateAllAsync(cancellationToken);
    return Results.Ok(recalculated);
});

leaderboards.MapGet("/{leaderboardType}", async (
    string leaderboardType,
    int? limit,
    ILeaderboardService leaderboardService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var board = await leaderboardService.GetLeaderboardAsync(leaderboardType, limit ?? 50, cancellationToken);
        return Results.Ok(board);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

leaderboards.MapGet("/{leaderboardType}/player/{playerId:guid}", async (
    string leaderboardType,
    Guid playerId,
    ILeaderboardService leaderboardService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var position = await leaderboardService.GetPlayerPositionAsync(playerId, leaderboardType, cancellationToken);
        return position is null ? Results.NotFound() : Results.Ok(position);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

leaderboards.MapGet("/{leaderboardType}/player/{playerId:guid}/history", async (
    string leaderboardType,
    Guid playerId,
    int? limit,
    ILeaderboardService leaderboardService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var history = await leaderboardService.GetHistoryAsync(playerId, leaderboardType, limit ?? 20, cancellationToken);
        return Results.Ok(history);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

leaderboards.MapPost("/{leaderboardType}/reset", async (
    HttpContext context,
    string leaderboardType,
    ILeaderboardService leaderboardService,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken) =>
{
    var denied = await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole],
        cancellationToken);
    if (denied is not null)
    {
        return denied;
    }

    try
    {
        var removed = await leaderboardService.ResetLeaderboardAsync(leaderboardType, cancellationToken);
        return Results.Ok(new { removed });
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

var strategic = app.MapGroup("/api/strategic")
    .WithTags("Strategic Systems");

strategic.MapGet("/volatility", async (
    Guid? sectorId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var cycles = await strategicService.GetSectorVolatilityCyclesAsync(sectorId, cancellationToken);
    return Results.Ok(cycles);
});

strategic.MapPost("/volatility", async (
    UpsertSectorVolatilityApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.UpsertSectorVolatilityCycleAsync(new UpdateSectorVolatilityCycleRequest
    {
        SectorId = request.SectorId,
        CurrentPhase = request.CurrentPhase,
        VolatilityIndex = request.VolatilityIndex,
        NextTransitionAt = request.NextTransitionAt
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/corporate-wars", async (
    bool? activeOnly,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var wars = await strategicService.GetCorporateWarsAsync(activeOnly ?? true, cancellationToken);
    return Results.Ok(wars);
});

strategic.MapPost("/corporate-wars", async (
    DeclareCorporateWarApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.DeclareCorporateWarAsync(new DeclareCorporateWarRequest
    {
        AttackerFactionId = request.AttackerFactionId,
        DefenderFactionId = request.DefenderFactionId,
        CasusBelli = request.CasusBelli,
        Intensity = request.Intensity
    }, cancellationToken);

    return result is null ? Results.BadRequest(new { error = "Unable to declare corporate war for the provided factions." }) : Results.Ok(result);
});

strategic.MapGet("/infrastructure", async (
    Guid? sectorId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var ownership = await strategicService.GetInfrastructureOwnershipAsync(sectorId, cancellationToken);
    return Results.Ok(ownership);
});

strategic.MapPost("/infrastructure", async (
    UpsertInfrastructureOwnershipApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.UpsertInfrastructureOwnershipAsync(new UpdateInfrastructureOwnershipRequest
    {
        SectorId = request.SectorId,
        FactionId = request.FactionId,
        InfrastructureType = request.InfrastructureType,
        ControlScore = request.ControlScore
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/territory-dominance", async (
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var dominance = await strategicService.GetTerritoryDominanceAsync(cancellationToken);
    return Results.Ok(dominance);
});

strategic.MapPost("/territory-dominance/recalculate/{factionId:guid}", async (
    Guid factionId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.RecalculateTerritoryDominanceAsync(factionId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/territory-economic-policy", async (
    Guid? factionId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var policies = await strategicService.GetTerritoryEconomicPoliciesAsync(factionId, cancellationToken);
    return Results.Ok(policies);
});

strategic.MapPost("/territory-economic-policy", async (
    UpsertTerritoryEconomicPolicyApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.UpsertTerritoryEconomicPolicyAsync(new UpsertTerritoryEconomicPolicyRequest
    {
        FactionId = request.FactionId,
        TaxRate = request.TaxRatePercent / 100m,
        TradeIncentiveModifier = request.TradeIncentivePercent / 100m
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/insurance/policies/{playerId:guid}", async (
    Guid playerId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var policies = await strategicService.GetInsurancePoliciesAsync(playerId, cancellationToken);
    return Results.Ok(policies);
});

strategic.MapPost("/insurance/policies", async (
    UpsertInsurancePolicyApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.UpsertInsurancePolicyAsync(new UpsertInsurancePolicyRequest
    {
        PlayerId = request.PlayerId,
        ShipId = request.ShipId,
        CoverageRate = request.CoverageRate,
        PremiumPerCycle = request.PremiumPerCycle,
        RiskTier = request.RiskTier,
        IsActive = request.IsActive
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/insurance/claims/{playerId:guid}", async (
    Guid playerId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var claims = await strategicService.GetInsuranceClaimsAsync(playerId, cancellationToken);
    return Results.Ok(claims);
});

strategic.MapPost("/insurance/claims", async (
    FileInsuranceClaimApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.FileInsuranceClaimAsync(new FileInsuranceClaimRequest
    {
        PolicyId = request.PolicyId,
        CombatLogId = request.CombatLogId,
        ClaimAmount = request.ClaimAmount
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapPost("/intelligence/networks", async (
    CreateIntelligenceNetworkApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.CreateIntelligenceNetworkAsync(new CreateIntelligenceNetworkRequest
    {
        OwnerPlayerId = request.OwnerPlayerId,
        Name = request.Name,
        AssetCount = request.AssetCount,
        CoverageScore = request.CoverageScore
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapPost("/intelligence/reports", async (
    PublishIntelligenceReportApiRequest request,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var result = await strategicService.PublishIntelligenceReportAsync(new PublishIntelligenceReportRequest
    {
        NetworkId = request.NetworkId,
        SectorId = request.SectorId,
        SignalType = request.SignalType,
        ConfidenceScore = request.ConfidenceScore,
        Payload = request.Payload,
        TtlMinutes = request.TtlMinutes
    }, cancellationToken);

    return result is null ? Results.NotFound() : Results.Ok(result);
});

strategic.MapGet("/intelligence/reports/{playerId:guid}", async (
    Guid playerId,
    Guid? sectorId,
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var reports = await strategicService.GetIntelligenceReportsAsync(playerId, sectorId, cancellationToken);
    return Results.Ok(reports);
});

strategic.MapPost("/intelligence/reports/expire", async (
    IStrategicSystemsService strategicService,
    CancellationToken cancellationToken) =>
{
    var expired = await strategicService.ExpireIntelligenceReportsAsync(cancellationToken);
    return Results.Ok(new { expired });
});

strategic.Map("/ws/dashboard/{playerId:guid}", async (
    HttpContext context,
    Guid playerId,
    int? intervalSeconds,
    IDashboardRealtimeSnapshotService dashboardRealtimeSnapshotService,
    CancellationToken cancellationToken) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "WebSocket upgrade required." }, cancellationToken);
        return;
    }

    var interval = TimeSpan.FromSeconds(Math.Clamp(intervalSeconds ?? 5, 2, 30));
    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    try
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var snapshot = await dashboardRealtimeSnapshotService.BuildSnapshotAsync(playerId, cancellationToken: cancellationToken);
            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(snapshot));
            await socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            await Task.Delay(interval, cancellationToken);
        }
    }
    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
    }
    catch (WebSocketException)
    {
    }
    finally
    {
        if (socket.State is WebSocketState.Open or WebSocketState.CloseReceived)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", CancellationToken.None);
        }
    }
});

static bool IsAdminAuthorized(HttpContext context, IConfiguration configuration)
{
    var expectedKey = configuration["Admin:Key"]
        ?? configuration["Admin__Key"]
        ?? "dev-admin-key";

    if (!context.Request.Headers.TryGetValue("X-Admin-Key", out var providedKey))
    {
        return false;
    }

    return string.Equals(providedKey.ToString(), expectedKey, StringComparison.Ordinal);
}

var adminBalance = app.MapGroup("/api/admin/balance")
    .WithTags("Admin - Balance Controls");

adminBalance.MapGet("/state", (HttpContext context, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(balanceControlService.GetSnapshot());
});

adminBalance.MapPost("/tax", (HttpContext context, UpdateTaxRateRequest request, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(balanceControlService.SetTaxRate(request.TaxRatePercent));
});

adminBalance.MapPost("/pirates", (HttpContext context, UpdatePirateIntensityRequest request, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(balanceControlService.SetPirateIntensity(request.IntensityPercent));
});

adminBalance.MapPost("/liquidity", (HttpContext context, LiquidityAdjustmentRequest request, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(balanceControlService.ApplyLiquidityAdjustment(request.DeltaPercent, request.Reason ?? "manual"));
});

adminBalance.MapPost("/instability", (HttpContext context, SectorInstabilityRequest request, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    if (request.SectorId == Guid.Empty)
    {
        return Results.BadRequest(new { error = "sectorId is required." });
    }

    return Results.Ok(balanceControlService.TriggerSectorInstability(request.SectorId, request.Reason ?? "manual"));
});

adminBalance.MapPost("/correction", (HttpContext context, EconomicCorrectionRequest request, IBalanceControlService balanceControlService, IConfiguration configuration) =>
{
    if (!IsAdminAuthorized(context, configuration))
    {
        return Results.Unauthorized();
    }

    return Results.Ok(balanceControlService.TriggerEconomicCorrection(request.AdjustmentPercent, request.Reason ?? "manual"));
});

var communication = app.MapGroup("/api/communication")
    .WithTags("Communication");

communication.MapPost("/subscribe", async (
    SubscribeChannelRequest request,
    ICommunicationService communicationService,
    CancellationToken cancellationToken) =>
{
    var result = await communicationService.SubscribeAsync(request, cancellationToken);
    return Results.Ok(result);
});

communication.MapPost("/unsubscribe", async (
    SubscribeChannelRequest request,
    ICommunicationService communicationService,
    CancellationToken cancellationToken) =>
{
    var result = await communicationService.UnsubscribeAsync(request, cancellationToken);
    return Results.Ok(result);
});

communication.MapPost("/messages", async (
    SendChannelMessageRequest request,
    ICommunicationService communicationService,
    CancellationToken cancellationToken) =>
{
    try
    {
        var message = await communicationService.SendMessageAsync(request, cancellationToken);
        return message is null ? Results.BadRequest() : Results.Ok(message);
    }
    catch (InvalidOperationException exception)
    {
        return Results.BadRequest(new { error = exception.Message });
    }
});

communication.MapGet("/messages/{channelType}/{channelKey}", async (
    string channelType,
    string channelKey,
    int? limit,
    ICommunicationService communicationService,
    CancellationToken cancellationToken) =>
{
    if (!Enum.TryParse<ChannelType>(channelType, true, out var parsedChannelType))
    {
        return Results.BadRequest(new { error = "Unsupported channel type." });
    }

    var messages = await communicationService.GetRecentMessagesAsync(parsedChannelType, channelKey, limit ?? 50, cancellationToken);
    return Results.Ok(messages);
});

var voice = communication.MapGroup("/voice");

voice.MapPost("/channels", async (
    CreateVoiceChannelRequest request,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var channel = await voiceService.CreateChannelAsync(request, cancellationToken);
    return Results.Created($"/api/communication/voice/channels/{channel.ChannelId}", channel);
});

voice.MapGet("/channels/{channelId:guid}", async (
    Guid channelId,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var channel = await voiceService.GetChannelAsync(channelId, cancellationToken);
    return channel is null ? Results.NotFound() : Results.Ok(channel);
});

voice.MapPost("/channels/{channelId:guid}/join", async (
    Guid channelId,
    JoinVoiceChannelRequest request,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var joined = await voiceService.JoinChannelAsync(channelId, request, cancellationToken);
    return joined is null ? Results.NotFound() : Results.Ok(joined);
});

voice.MapPost("/channels/{channelId:guid}/leave/{playerId:guid}", async (
    Guid channelId,
    Guid playerId,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var left = await voiceService.LeaveChannelAsync(channelId, playerId, cancellationToken);
    return left ? Results.NoContent() : Results.NotFound();
});

voice.MapPost("/channels/{channelId:guid}/signal", async (
    Guid channelId,
    VoiceSignalRequest request,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var signal = await voiceService.PublishSignalAsync(channelId, request, cancellationToken);
    return signal is null ? Results.NotFound() : Results.Ok(signal);
});

voice.MapGet("/channels/{channelId:guid}/signals/{playerId:guid}", async (
    Guid channelId,
    Guid playerId,
    int? limit,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var signals = await voiceService.DequeueSignalsAsync(channelId, playerId, limit ?? 50, cancellationToken);
    return Results.Ok(signals);
});

voice.MapPost("/channels/{channelId:guid}/activity", async (
    Guid channelId,
    VoiceActivityRequest request,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var activity = await voiceService.UpdateActivityAsync(channelId, request, cancellationToken);
    return activity is null ? Results.NotFound() : Results.Ok(activity);
});

voice.MapPost("/channels/{channelId:guid}/spatial-audio", async (
    Guid channelId,
    SpatialAudioRequest request,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var mix = await voiceService.CalculateSpatialMixAsync(channelId, request, cancellationToken);
    return mix is null ? Results.NotFound() : Results.Ok(mix);
});

voice.MapGet("/channels/{channelId:guid}/qos", async (
    Guid channelId,
    IVoiceService voiceService,
    CancellationToken cancellationToken) =>
{
    var qos = await voiceService.GetQosSnapshotAsync(channelId, cancellationToken);
    return qos is null ? Results.NotFound() : Results.Ok(qos);
});

communication.Map("/ws/{channelType}/{channelKey}", async (
    HttpContext context,
    string channelType,
    string channelKey,
    ICommunicationService communicationService,
    CancellationToken cancellationToken) =>
{
    if (!context.WebSockets.IsWebSocketRequest)
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "WebSocket upgrade required." }, cancellationToken);
        return;
    }

    if (!Enum.TryParse<ChannelType>(channelType, true, out var parsedChannelType))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "Unsupported channel type." }, cancellationToken);
        return;
    }

    if (!Guid.TryParse(context.Request.Query["playerId"], out var playerId))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "Query string playerId is required." }, cancellationToken);
        return;
    }

    await communicationService.SubscribeAsync(new SubscribeChannelRequest
    {
        PlayerId = playerId,
        ChannelType = parsedChannelType,
        ChannelKey = channelKey
    }, cancellationToken);

    using var socket = await context.WebSockets.AcceptWebSocketAsync();
    var connectionId = Guid.NewGuid();
    var normalizedKey = channelKey.Trim().ToLowerInvariant();
    channelSockets[connectionId] = (socket, parsedChannelType, normalizedKey);

    var backlog = await communicationService.GetRecentMessagesAsync(parsedChannelType, normalizedKey, 25, cancellationToken);
    var backlogBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(backlog));
    await socket.SendAsync(backlogBytes, WebSocketMessageType.Text, true, cancellationToken);

    var buffer = new byte[4096];
    try
    {
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            var receiveResult = await socket.ReceiveAsync(buffer, cancellationToken);
            if (receiveResult.MessageType == WebSocketMessageType.Close)
            {
                break;
            }

            if (receiveResult.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            var content = Encoding.UTF8.GetString(buffer, 0, receiveResult.Count).Trim();
            if (string.IsNullOrWhiteSpace(content))
            {
                continue;
            }

            ChannelMessageDto? created;
            try
            {
                created = await communicationService.SendMessageAsync(new SendChannelMessageRequest
                {
                    PlayerId = playerId,
                    ChannelType = parsedChannelType,
                    ChannelKey = normalizedKey,
                    Content = content
                }, cancellationToken);
            }
            catch (InvalidOperationException exception)
            {
                var rateLimitedBytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { error = exception.Message }));
                await socket.SendAsync(rateLimitedBytes, WebSocketMessageType.Text, true, cancellationToken);
                continue;
            }

            if (created is null)
            {
                continue;
            }

            var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(created));
            foreach (var entry in channelSockets.Values)
            {
                if (entry.Socket.State != WebSocketState.Open)
                {
                    continue;
                }

                if (entry.ChannelType != parsedChannelType || entry.ChannelKey != normalizedKey)
                {
                    continue;
                }

                await entry.Socket.SendAsync(payload, WebSocketMessageType.Text, true, cancellationToken);
            }
        }
    }
    finally
    {
        channelSockets.TryRemove(connectionId, out _);
        if (socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "closing", cancellationToken);
        }

        await communicationService.UnsubscribeAsync(new SubscribeChannelRequest
        {
            PlayerId = playerId,
            ChannelType = parsedChannelType,
            ChannelKey = normalizedKey
        }, cancellationToken);
    }
});

app.Run();

static async Task<(bool Success, bool InvalidCredentials, (string AccessToken, DateTimeOffset ExpiresAtUtc, string KeycloakUserId, Guid KeycloakUserIdAsGuid, string Username, string Email, IReadOnlyCollection<string> Roles)? Session)> TryLoginAgainstKeycloakAsync(
    string username,
    string password,
    KeycloakOptions options,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken)
{
    if (string.IsNullOrWhiteSpace(options.ServerUrl) ||
        string.IsNullOrWhiteSpace(options.Realm) ||
        string.IsNullOrWhiteSpace(options.ClientId))
    {
        return (false, false, null);
    }

    var tokenEndpoint = $"{options.ServerUrl.TrimEnd('/')}/realms/{Uri.EscapeDataString(options.Realm.Trim())}/protocol/openid-connect/token";
    var payload = new Dictionary<string, string>
    {
        ["grant_type"] = "password",
        ["client_id"] = options.ClientId.Trim(),
        ["username"] = username.Trim(),
        ["password"] = password,
        ["scope"] = "openid profile email"
    };

    if (!string.IsNullOrWhiteSpace(options.ClientSecret))
    {
        payload["client_secret"] = options.ClientSecret.Trim();
    }

    using var form = new FormUrlEncodedContent(payload);
    return await ExecuteKeycloakLoginRequestAsync(
        tokenEndpoint,
        form,
        username,
        httpClientFactory,
        cancellationToken);
}

static async Task<(bool Success, bool InvalidCredentials, (string AccessToken, DateTimeOffset ExpiresAtUtc, string KeycloakUserId, Guid KeycloakUserIdAsGuid, string Username, string Email, IReadOnlyCollection<string> Roles)? Session)> ExecuteKeycloakLoginRequestAsync(
    string tokenEndpoint,
    FormUrlEncodedContent form,
    string requestedUsername,
    IHttpClientFactory httpClientFactory,
    CancellationToken cancellationToken)
{
    using var httpClient = httpClientFactory.CreateClient();
    HttpResponseMessage response;
    try
    {
        response = await httpClient.PostAsync(tokenEndpoint, form, cancellationToken);
    }
    catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
    {
        return (false, false, null);
    }
    catch (HttpRequestException)
    {
        return (false, false, null);
    }

    if (response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.Unauthorized)
    {
        return (false, true, null);
    }

    if (!response.IsSuccessStatusCode)
    {
        return (false, false, null);
    }

    using var payloadJson = await response.Content.ReadFromJsonAsync<JsonDocument>(cancellationToken);
    if (payloadJson is null ||
        !payloadJson.RootElement.TryGetProperty("access_token", out var accessTokenElement) ||
        accessTokenElement.ValueKind != JsonValueKind.String)
    {
        return (false, false, null);
    }

    var accessToken = accessTokenElement.GetString();
    if (string.IsNullOrWhiteSpace(accessToken))
    {
        return (false, false, null);
    }

    var expiresInSeconds =
        payloadJson.RootElement.TryGetProperty("expires_in", out var expiresElement) &&
        expiresElement.ValueKind == JsonValueKind.Number &&
        expiresElement.TryGetInt32(out var parsedSeconds)
            ? parsedSeconds
            : 0;

    JwtSecurityToken jwt;
    try
    {
        jwt = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
    }
    catch (ArgumentException)
    {
        return (false, false, null);
    }

    var keycloakUserId = jwt.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value;
    if (string.IsNullOrWhiteSpace(keycloakUserId))
    {
        return (false, false, null);
    }

    var resolvedUsername =
        jwt.Claims.FirstOrDefault(claim => claim.Type == "preferred_username")?.Value
        ?? requestedUsername.Trim();
    var resolvedEmail =
        jwt.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Email)?.Value
        ?? jwt.Claims.FirstOrDefault(claim => claim.Type == "email")?.Value
        ?? $"{resolvedUsername}@local.invalid";

    var expiresAtUtc = expiresInSeconds > 0
        ? DateTimeOffset.UtcNow.AddSeconds(expiresInSeconds)
        : (jwt.ValidTo > DateTime.MinValue ? new DateTimeOffset(jwt.ValidTo) : DateTimeOffset.UtcNow.AddHours(1));

    var roles = ExtractRolesFromToken(jwt);
    var keycloakGuid = ParseKeycloakSubjectAsGuid(keycloakUserId);

    return (true, false, (accessToken, expiresAtUtc, keycloakUserId, keycloakGuid, resolvedUsername, resolvedEmail, roles));
}

static IReadOnlyCollection<string> ExtractRolesFromToken(JwtSecurityToken token)
{
    var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    foreach (var claim in token.Claims.Where(claim =>
                 claim.Type == ClaimTypes.Role ||
                 claim.Type.Equals("role", StringComparison.OrdinalIgnoreCase) ||
                 claim.Type.Equals("roles", StringComparison.OrdinalIgnoreCase)))
    {
        foreach (var role in claim.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            roles.Add(role);
        }
    }

    var realmAccess = token.Claims.FirstOrDefault(claim => claim.Type.Equals("realm_access", StringComparison.OrdinalIgnoreCase))?.Value;
    if (!string.IsNullOrWhiteSpace(realmAccess))
    {
        try
        {
            using var json = JsonDocument.Parse(realmAccess);
            if (json.RootElement.TryGetProperty("roles", out var roleArray) &&
                roleArray.ValueKind == JsonValueKind.Array)
            {
                foreach (var roleElement in roleArray.EnumerateArray())
                {
                    if (roleElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var role = roleElement.GetString();
                    if (!string.IsNullOrWhiteSpace(role))
                    {
                        roles.Add(role);
                    }
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed realm access role payloads.
        }
    }

    return roles.ToList();
}

static Guid ParseKeycloakSubjectAsGuid(string subject)
{
    if (Guid.TryParse(subject, out var parsed))
    {
        return parsed;
    }

    var hash = SHA256.HashData(Encoding.UTF8.GetBytes(subject));
    var guidBytes = new byte[16];
    Array.Copy(hash, guidBytes, guidBytes.Length);
    return new Guid(guidBytes);
}

static async Task<PlayerIdentity> EnsurePlayerIdentityForKeycloakLoginAsync(
    GalacticTraderDbContext dbContext,
    string keycloakUserId,
    Guid keycloakUserIdAsGuid,
    string username,
    string email,
    IReadOnlyCollection<string> requiredRoles,
    CancellationToken cancellationToken)
{
    var normalizedUsername = username.Trim();
    var normalizedEmail = email.Trim();

    var existingUserAccount = await dbContext.UserAccounts
        .AsNoTracking()
        .FirstOrDefaultAsync(
            account =>
                account.KeycloakId == keycloakUserId ||
                account.Username == normalizedUsername ||
                account.Email == normalizedEmail,
            cancellationToken);

    var existingPlayer = await dbContext.Players
        .AsNoTracking()
        .FirstOrDefaultAsync(
            player =>
                player.KeycloakUserId == keycloakUserIdAsGuid ||
                player.Username == normalizedUsername ||
                player.Email == normalizedEmail,
            cancellationToken);

    var playerId = existingPlayer?.Id ?? existingUserAccount?.Id ?? Guid.NewGuid();

    await EnsureUserAccountRolesAsync(
        dbContext,
        playerId,
        normalizedUsername,
        normalizedEmail,
        requiredRoles,
        cancellationToken,
        keycloakUserId);

    var registeredAt = existingPlayer is null
        ? DateTimeOffset.UtcNow
        : new DateTimeOffset(DateTime.SpecifyKind(existingPlayer.CreatedAt, DateTimeKind.Utc));

    return new PlayerIdentity(playerId, normalizedUsername, normalizedEmail, registeredAt);
}

static async Task<IResult?> RequireMapAdminAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    return await RequireAnyRoleAsync(
        context,
        authService,
        dbContext,
        [AuthorizationPolicies.AdminRole, AuthorizationPolicies.MapAdminRole],
        cancellationToken);
}

static async Task<IResult?> RequireAnyRoleAsync(
    HttpContext context,
    IAuthService authService,
    GalacticTraderDbContext dbContext,
    IReadOnlyCollection<string> allowedRoles,
    CancellationToken cancellationToken)
{
    if (!TryReadBearerToken(context, out var token))
    {
        return Results.Unauthorized();
    }

    var session = await authService.ValidateTokenAsync(token, cancellationToken);
    if (session is not null)
    {
        var userAccount = await dbContext.UserAccounts
            .AsNoTracking()
            .FirstOrDefaultAsync(
                account =>
                    account.Username == session.Player.Username ||
                    account.Email == session.Player.Email,
                cancellationToken);

        if (userAccount is null)
        {
            return Results.StatusCode(StatusCodes.Status403Forbidden);
        }

        var hasAllowedRole = userAccount.Roles.Any(role =>
            allowedRoles.Any(allowed => role.Equals(allowed, StringComparison.OrdinalIgnoreCase)));

        return hasAllowedRole
            ? null
            : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    if (!LooksLikeJwt(token))
    {
        return Results.Unauthorized();
    }

    var tokenValidationService = context.RequestServices.GetRequiredService<ITokenValidationService>();

    var principal = await tokenValidationService.ValidateTokenAsync(token);
    if (principal is null)
    {
        return Results.Unauthorized();
    }

    var tokenRoles = tokenValidationService.GetRoles(principal);
    var hasAllowedJwtRole = tokenRoles.Any(role =>
        allowedRoles.Any(allowed => role.Equals(allowed, StringComparison.OrdinalIgnoreCase)));

    return hasAllowedJwtRole
        ? null
        : Results.StatusCode(StatusCodes.Status403Forbidden);
}

static bool LooksLikeJwt(string token)
{
    return token.Count(character => character == '.') == 2;
}

static bool TryReadBearerToken(HttpContext context, out string token)
{
    token = string.Empty;
    if (!context.Request.Headers.TryGetValue("Authorization", out var authorizationHeader))
    {
        return false;
    }

    var headerValue = authorizationHeader.ToString();
    const string prefix = "Bearer ";
    if (!headerValue.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
    {
        return false;
    }

    var extractedToken = headerValue[prefix.Length..].Trim();
    if (string.IsNullOrWhiteSpace(extractedToken))
    {
        return false;
    }

    token = extractedToken;
    return true;
}

static async Task EnsureBootstrapAdminPlayerAsync(
    GalacticTraderDbContext dbContext,
    IAuthService authService,
    IConfiguration configuration,
    CancellationToken cancellationToken)
{
    const string bootstrapUsername = "viper";
    const string bootstrapEmail = "epiphanygs@gmail.com";

    var configuredPassword = configuration["Bootstrap:ViperPassword"]
        ?? configuration["Bootstrap__ViperPassword"];
    var bootstrapPassword = string.IsNullOrWhiteSpace(configuredPassword)
        ? "ViperDev123!"
        : configuredPassword.Trim();

    var existingPlayer = await dbContext.Players
        .AsNoTracking()
        .FirstOrDefaultAsync(
            player =>
                player.Username == bootstrapUsername ||
                player.Email == bootstrapEmail,
            cancellationToken);

    var bootstrapPlayerId = existingPlayer?.Id ?? Guid.NewGuid();
    PlayerIdentity identity;
    try
    {
        identity = await authService.RegisterAsync(
            new RegisterPlayerRequest(
                bootstrapUsername,
                bootstrapEmail,
                bootstrapPassword,
                FirstName: "Viper",
                LastName: "Pilot",
                PlayerId: bootstrapPlayerId),
            cancellationToken);
    }
    catch (InvalidOperationException)
    {
        identity = new PlayerIdentity(
            bootstrapPlayerId,
            bootstrapUsername,
            bootstrapEmail,
            DateTimeOffset.UtcNow);
    }

    await EnsureUserAccountRolesAsync(
        dbContext,
        identity.PlayerId,
        identity.Username,
        identity.Email,
        [AuthorizationPolicies.PlayerRole, AuthorizationPolicies.AdminRole, AuthorizationPolicies.MapAdminRole],
        cancellationToken);

    await BootstrapNewPlayerAsync(dbContext, identity, cancellationToken);
}

static async Task EnsureUserAccountRolesAsync(
    GalacticTraderDbContext dbContext,
    Guid playerId,
    string username,
    string email,
    IReadOnlyCollection<string> requiredRoles,
    CancellationToken cancellationToken,
    string? keycloakId = null)
{
    var normalizedUsername = username.Trim();
    var normalizedEmail = email.Trim();
    var resolvedKeycloakId = string.IsNullOrWhiteSpace(keycloakId)
        ? playerId.ToString("D")
        : keycloakId.Trim();

    var userAccount = await dbContext.UserAccounts
        .FirstOrDefaultAsync(
            account =>
                account.Id == playerId ||
                account.Username == normalizedUsername ||
                account.Email == normalizedEmail,
            cancellationToken);

    if (userAccount is null)
    {
        userAccount = new GalacticTrader.Data.Models.UserAccount
        {
            Id = playerId,
            Username = normalizedUsername,
            Email = normalizedEmail,
            FirstName = normalizedUsername,
            LastName = "Pilot",
            KeycloakId = resolvedKeycloakId,
            Roles = []
        };

        dbContext.UserAccounts.Add(userAccount);
    }
    else
    {
        userAccount.Username = normalizedUsername;
        userAccount.Email = normalizedEmail;
        userAccount.KeycloakId = resolvedKeycloakId;
    }

    foreach (var role in requiredRoles.Where(role => !string.IsNullOrWhiteSpace(role)))
    {
        if (userAccount.Roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            continue;
        }

        userAccount.Roles.Add(role);
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}

static async Task BootstrapNewPlayerAsync(
    GalacticTraderDbContext dbContext,
    PlayerIdentity identity,
    CancellationToken cancellationToken,
    Guid? keycloakUserId = null)
{
    const decimal starterCredits = 250_000m;
    const decimal starterShipValue = 95_000m;
    var now = DateTime.UtcNow;
    var resolvedKeycloakUserId = keycloakUserId ?? identity.PlayerId;

    var starterSector = await EnsureStarterSectorAsync(dbContext, cancellationToken);
    var player = await dbContext.Players
        .FirstOrDefaultAsync(
            existing =>
                existing.Id == identity.PlayerId ||
                existing.Username == identity.Username ||
                existing.Email == identity.Email,
            cancellationToken);

    if (player is null)
    {
        player = new Player
        {
            Id = identity.PlayerId,
            Username = identity.Username,
            Email = identity.Email,
            KeycloakUserId = resolvedKeycloakUserId,
            NetWorth = starterCredits + starterShipValue,
            LiquidCredits = starterCredits,
            ReputationScore = 0,
            AlignmentLevel = 0,
            FleetStrengthRating = 342,
            ProtectionStatus = "Protected",
            CreatedAt = now,
            LastActiveAt = now,
            IsActive = true
        };

        dbContext.Players.Add(player);
    }
    else
    {
        player.Username = identity.Username;
        player.Email = identity.Email;
        if (keycloakUserId.HasValue || player.KeycloakUserId == Guid.Empty)
        {
            player.KeycloakUserId = resolvedKeycloakUserId;
        }
        player.LastActiveAt = now;
        player.IsActive = true;
        player.ProtectionStatus = string.IsNullOrWhiteSpace(player.ProtectionStatus)
            ? "Protected"
            : player.ProtectionStatus;
    }

    if (player.LiquidCredits < starterCredits)
    {
        player.LiquidCredits = starterCredits;
    }

    var hasShip = await dbContext.Ships
        .AnyAsync(ship => ship.PlayerId == player.Id, cancellationToken);
    if (!hasShip)
    {
        var starterShip = new Ship
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            Name = "Pioneer-01",
            ShipClass = "Scout",
            HullIntegrity = 180,
            MaxHullIntegrity = 180,
            ShieldCapacity = 120,
            MaxShieldCapacity = 120,
            ReactorOutput = 90,
            CargoCapacity = 160,
            CargoUsed = 0,
            SensorRange = 120,
            SignatureProfile = 35,
            CrewSlots = 6,
            Hardpoints = 2,
            HasInsurance = true,
            InsuranceRate = 0.015m,
            IsActive = true,
            IsInCombat = false,
            CurrentSectorId = starterSector.Id,
            TargetSectorId = starterSector.Id,
            StatusId = 0,
            PurchasePrice = starterShipValue,
            PurchasedAt = now,
            CurrentValue = starterShipValue
        };

        dbContext.Ships.Add(starterShip);
        hasShip = true;
    }

    var minimumNetWorth = player.LiquidCredits + (hasShip ? starterShipValue : 0m);
    if (player.NetWorth < minimumNetWorth)
    {
        player.NetWorth = minimumNetWorth;
    }

    await dbContext.SaveChangesAsync(cancellationToken);
}

static async Task<Sector> EnsureStarterSectorAsync(
    GalacticTraderDbContext dbContext,
    CancellationToken cancellationToken)
{
    var existing = await dbContext.Sectors
        .OrderByDescending(sector => sector.SecurityLevel)
        .ThenByDescending(sector => sector.EconomicIndex)
        .FirstOrDefaultAsync(cancellationToken);
    if (existing is not null)
    {
        return existing;
    }

    var sector = new Sector
    {
        Id = Guid.NewGuid(),
        Name = "New Dawn",
        X = 0,
        Y = 0,
        Z = 0,
        SecurityLevel = 90,
        HazardRating = 8,
        ResourceModifier = 1.0f,
        EconomicIndex = 85,
        SensorInterferenceLevel = 5.0f,
        ControlledByFactionId = null,
        AverageTrafficLevel = 70,
        PiratePresenceProbability = 5
    };

    dbContext.Sectors.Add(sector);
    await dbContext.SaveChangesAsync(cancellationToken);
    return sector;
}

public partial class Program;
