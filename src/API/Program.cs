using GalacticTrader.API.Telemetry;
using GalacticTrader.API.Swagger;
using GalacticTrader.API.Secrets;
using GalacticTrader.Data;
using GalacticTrader.Data.Repositories.Navigation;
using GalacticTrader.Services.Caching;
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
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Prometheus;
using System.Collections.Concurrent;
using System.Diagnostics;
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
builder.Services.AddSingleton<IAuthService, AuthService>();
builder.Services.AddSingleton<IVoiceService, VoiceService>();
builder.Services.AddHostedService<TelemetryGaugeRefreshService>();

var app = builder.Build();

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

var auth = app.MapGroup("/api/auth")
    .WithTags("Authentication");

auth.MapPost("/register", async (
    RegisterPlayerApiRequest request,
    IAuthService authService,
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

    try
    {
        var created = await authService.RegisterAsync(
            new RegisterPlayerRequest(request.Username, request.Email, request.Password),
            cancellationToken);
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
        operation.Description = "Creates a test/dev player identity and stores credentials in the in-memory auth provider.";
        return operation;
    });

auth.MapPost("/login", async (
    LoginPlayerApiRequest request,
    IAuthService authService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { error = "Username and password are required." });
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

sectors.MapPost("/", async (CreateSectorRequest request, ISectorService service, CancellationToken cancellationToken) =>
{
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

sectors.MapPut("/{sectorId:guid}", async (Guid sectorId, UpdateSectorRequest request, ISectorService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateSectorAsync(
        sectorId,
        request.SecurityLevel,
        request.HazardRating,
        request.FactionId,
        cancellationToken);

    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

sectors.MapDelete("/{sectorId:guid}", async (Guid sectorId, ISectorService service, CancellationToken cancellationToken) =>
{
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

routes.MapPost("/", async (CreateRouteRequest request, IRouteService service, CancellationToken cancellationToken) =>
{
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

routes.MapPut("/{routeId:guid}", async (Guid routeId, UpdateRouteRequest request, IRouteService service, CancellationToken cancellationToken) =>
{
    var updated = await service.UpdateRouteAsync(routeId, request.LegalStatus, request.BaseRiskScore, cancellationToken);
    return updated is null ? Results.NotFound() : Results.Ok(updated);
});

routes.MapDelete("/{routeId:guid}", async (Guid routeId, IRouteService service, CancellationToken cancellationToken) =>
{
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
    IEconomyService economyService,
    CancellationToken cancellationToken) =>
{
    var tick = await economyService.ProcessMarketTickAsync(cancellationToken);
    return Results.Ok(tick);
});

economy.MapPost("/market-shock", async (
    MarketShockRequest request,
    IEconomyService economyService,
    CancellationToken cancellationToken) =>
{
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
    CreateNpcRequest request,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
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
    Guid agentId,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
    var result = await npcService.ProcessDecisionTickAsync(agentId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

npc.MapPost("/tick-all", async (INpcService npcService, CancellationToken cancellationToken) =>
{
    var result = await npcService.ProcessAllDecisionTicksAsync(cancellationToken);
    return Results.Ok(result);
});

npc.MapPost("/agents/{agentId:guid}/fleet/spawn", async (
    Guid agentId,
    int? ships,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
    var summary = await npcService.SpawnFleetAsync(agentId, ships ?? 3, cancellationToken);
    return summary is null ? Results.NotFound() : Results.Ok(summary);
});

npc.MapPost("/agents/{agentId:guid}/route/{targetSectorId:guid}", async (
    Guid agentId,
    Guid targetSectorId,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
    var planned = await npcService.PlanRouteAsync(agentId, targetSectorId, cancellationToken);
    return planned ? Results.Accepted() : Results.BadRequest(new { error = "Route planning failed." });
});

npc.MapPost("/agents/{agentId:guid}/move", async (
    Guid agentId,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
    var moved = await npcService.ProcessFleetMovementAsync(agentId, cancellationToken);
    return moved ? Results.Accepted() : Results.BadRequest(new { error = "Movement processing failed." });
});

npc.MapPost("/agents/{agentId:guid}/trade", async (
    Guid agentId,
    INpcService npcService,
    CancellationToken cancellationToken) =>
{
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

leaderboards.MapPost("/recalculate", async (ILeaderboardService leaderboardService, CancellationToken cancellationToken) =>
{
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
    string leaderboardType,
    ILeaderboardService leaderboardService,
    CancellationToken cancellationToken) =>
{
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

public sealed record CreateSectorRequest(string Name, float X, float Y, float Z);
public sealed record UpdateSectorRequest(int? SecurityLevel, int? HazardRating, Guid? FactionId);
public sealed record CreateRouteRequest(Guid FromSectorId, Guid ToSectorId, string LegalStatus, string WarpGateType);
public sealed record UpdateRouteRequest(string? LegalStatus, float? BaseRiskScore);
public sealed record RegisterPlayerApiRequest(string Username, string Email, string Password);
public sealed record LoginPlayerApiRequest(string Username, string Password);

public partial class Program;
