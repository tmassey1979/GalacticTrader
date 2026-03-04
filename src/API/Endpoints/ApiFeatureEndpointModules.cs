using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using GalacticTrader.API.Contracts;
using GalacticTrader.API.Telemetry;
using GalacticTrader.Data;
using GalacticTrader.Services.Admin;
using GalacticTrader.Services.Auth;
using GalacticTrader.Services.Authentication;
using GalacticTrader.Services.Communication;
using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Economy;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Leaderboard;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Npc;
using GalacticTrader.Services.Realtime;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;
using GalacticTrader.Services.Telemetry;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Prometheus;

namespace GalacticTrader.API.Endpoints;

public static class ApiFeatureEndpointModules
{
    public static void MapTelemetryEndpoints(this WebApplication app)
    {
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
    }

    public static void MapNavigationEndpoints(
        this WebApplication app,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, CancellationToken, Task<IResult?>> requireMapAdminAsync)
    {
        Task<IResult?> RequireMapAdminAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken)
            => requireMapAdminAsync(context, authService, dbContext, cancellationToken);

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

    }

    public static void MapCoreGameplayEndpoints(
        this WebApplication app,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, IReadOnlyCollection<string>, CancellationToken, Task<IResult?>> requireAnyRoleAsync,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, Guid, CancellationToken, Task<IResult?>> requireOwnerOrAdminAsync)
    {
        Task<IResult?> RequireAnyRoleAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken)
            => requireAnyRoleAsync(context, authService, dbContext, allowedRoles, cancellationToken);

        Task<IResult?> RequireOwnerOrAdminAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            Guid ownerPlayerId,
            CancellationToken cancellationToken)
            => requireOwnerOrAdminAsync(context, authService, dbContext, ownerPlayerId, cancellationToken);

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
            HttpContext context,
            Guid playerId,
            int? limit,
            IMarketTransactionService tradeService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            IFleetService fleetService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            FleetFormation? formation,
            IFleetService fleetService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            IReputationService reputationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            IReputationService reputationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            IReputationService reputationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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

    }

    public static void MapStrategicEndpoints(
        this WebApplication app,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, IReadOnlyCollection<string>, CancellationToken, Task<IResult?>> requireAnyRoleAsync,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, Guid, CancellationToken, Task<IResult?>> requireOwnerOrAdminAsync,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, CancellationToken, Task<(Guid? PlayerId, bool IsAdmin, IResult? Denied)>> resolveAuthenticatedActorAsync)
    {
        Task<IResult?> RequireAnyRoleAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            IReadOnlyCollection<string> allowedRoles,
            CancellationToken cancellationToken)
            => requireAnyRoleAsync(context, authService, dbContext, allowedRoles, cancellationToken);

        Task<IResult?> RequireOwnerOrAdminAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            Guid ownerPlayerId,
            CancellationToken cancellationToken)
            => requireOwnerOrAdminAsync(context, authService, dbContext, ownerPlayerId, cancellationToken);

        Task<(Guid? PlayerId, bool IsAdmin, IResult? Denied)> ResolveAuthenticatedActorAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken)
            => resolveAuthenticatedActorAsync(context, authService, dbContext, cancellationToken);

        var strategic = app.MapGroup("/api/strategic")
            .WithTags("Strategic Systems");

        strategic.MapGet("/terra/status", async (
            IStrategicSystemsService strategicService,
            CancellationToken cancellationToken) =>
        {
            var status = await strategicService.GetTerraColonistStatusAsync(cancellationToken);
            return Results.Ok(status);
        });

        strategic.MapPost("/terra/status", async (
            HttpContext context,
            UpdateTerraColonistSourceApiRequest request,
            IStrategicSystemsService strategicService,
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

            var updated = await strategicService.UpdateTerraColonistSourceAsync(new UpdateTerraColonistSourceRequest
            {
                SectorId = request.SectorId,
                OutputPerMinute = request.OutputPerMinute,
                StorageCapacity = request.StorageCapacity,
                AvailableColonists = request.AvailableColonists
            }, cancellationToken);

            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        strategic.MapPost("/terra/shipments", async (
            HttpContext context,
            CreateColonistShipmentApiRequest request,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var created = await strategicService.CreateColonistShipmentAsync(new CreateColonistShipmentRequest
            {
                PlayerId = request.PlayerId,
                DestinationSectorId = request.DestinationSectorId,
                ColonistCount = request.ColonistCount,
                TravelMode = request.TravelMode,
                Algorithm = request.Algorithm
            }, cancellationToken);

            return created is null
                ? Results.BadRequest(new { error = "Unable to create colonist shipment with provided parameters." })
                : Results.Created($"/api/strategic/terra/shipments/{created.Id:D}", created);
        });

        strategic.MapGet("/terra/shipments/{playerId:guid}", async (
            HttpContext context,
            Guid playerId,
            bool? includeDelivered,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var shipments = await strategicService.GetColonistShipmentsAsync(
                playerId,
                includeDelivered ?? true,
                cancellationToken);
            return Results.Ok(shipments);
        });

        strategic.MapGet("/terra/history/{playerId:guid}", async (
            HttpContext context,
            Guid playerId,
            int? limit,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var history = await strategicService.GetColonistDeliveryHistoryAsync(
                playerId,
                limit ?? 50,
                cancellationToken);
            return Results.Ok(history);
        });

        strategic.MapPost("/terra/process-arrivals", async (
            HttpContext context,
            Guid? playerId,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            if (playerId.HasValue && playerId.Value != Guid.Empty)
            {
                var denied = await RequireOwnerOrAdminAsync(
                    context,
                    authService,
                    dbContext,
                    playerId.Value,
                    cancellationToken);
                if (denied is not null)
                {
                    return denied;
                }
            }
            else
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
            }

            var processed = await strategicService.ProcessColonistArrivalsAsync(playerId, cancellationToken);
            return Results.Ok(new { processed });
        });

        strategic.MapGet("/terra/telemetry", async (
            HttpContext context,
            IStrategicSystemsService strategicService,
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

            var telemetry = await strategicService.GetTerraColonistTelemetryAsync(null, cancellationToken);
            return Results.Ok(telemetry);
        });

        strategic.MapGet("/terra/telemetry/{playerId:guid}", async (
            HttpContext context,
            Guid playerId,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var telemetry = await strategicService.GetTerraColonistTelemetryAsync(playerId, cancellationToken);
            return Results.Ok(telemetry);
        });

        strategic.MapGet("/volatility", async (
            Guid? sectorId,
            IStrategicSystemsService strategicService,
            CancellationToken cancellationToken) =>
        {
            var cycles = await strategicService.GetSectorVolatilityCyclesAsync(sectorId, cancellationToken);
            return Results.Ok(cycles);
        });

        strategic.MapPost("/volatility", async (
            HttpContext context,
            UpsertSectorVolatilityApiRequest request,
            IStrategicSystemsService strategicService,
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
            HttpContext context,
            DeclareCorporateWarApiRequest request,
            IStrategicSystemsService strategicService,
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
            HttpContext context,
            UpsertInfrastructureOwnershipApiRequest request,
            IStrategicSystemsService strategicService,
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
            HttpContext context,
            Guid factionId,
            IStrategicSystemsService strategicService,
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
            HttpContext context,
            UpsertTerritoryEconomicPolicyApiRequest request,
            IStrategicSystemsService strategicService,
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

            var result = await strategicService.UpsertTerritoryEconomicPolicyAsync(new UpsertTerritoryEconomicPolicyRequest
            {
                FactionId = request.FactionId,
                TaxRate = request.TaxRatePercent / 100m,
                TradeIncentiveModifier = request.TradeIncentivePercent / 100m
            }, cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        strategic.MapGet("/insurance/policies/{playerId:guid}", async (
            HttpContext context,
            Guid playerId,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var policies = await strategicService.GetInsurancePoliciesAsync(playerId, cancellationToken);
            return Results.Ok(policies);
        });

        strategic.MapPost("/insurance/policies", async (
            HttpContext context,
            UpsertInsurancePolicyApiRequest request,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            Guid playerId,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var claims = await strategicService.GetInsuranceClaimsAsync(playerId, cancellationToken);
            return Results.Ok(claims);
        });

        strategic.MapPost("/insurance/claims", async (
            HttpContext context,
            FileInsuranceClaimApiRequest request,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (callerPlayerId, isAdmin, denied) = await ResolveAuthenticatedActorAsync(
                context,
                authService,
                dbContext,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var policyOwnerId = await dbContext.InsurancePolicies
                .AsNoTracking()
                .Where(policy => policy.Id == request.PolicyId)
                .Select(policy => (Guid?)policy.PlayerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!isAdmin && !callerPlayerId.HasValue)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!isAdmin && policyOwnerId.HasValue && callerPlayerId!.Value != policyOwnerId.Value)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            var result = await strategicService.FileInsuranceClaimAsync(new FileInsuranceClaimRequest
            {
                PolicyId = request.PolicyId,
                CombatLogId = request.CombatLogId,
                ClaimAmount = request.ClaimAmount
            }, cancellationToken);

            return result is null ? Results.NotFound() : Results.Ok(result);
        });

        strategic.MapPost("/intelligence/networks", async (
            HttpContext context,
            CreateIntelligenceNetworkApiRequest request,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                request.OwnerPlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

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
            HttpContext context,
            PublishIntelligenceReportApiRequest request,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (callerPlayerId, isAdmin, denied) = await ResolveAuthenticatedActorAsync(
                context,
                authService,
                dbContext,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var networkOwnerId = await dbContext.IntelligenceNetworks
                .AsNoTracking()
                .Where(network => network.Id == request.NetworkId)
                .Select(network => (Guid?)network.OwnerPlayerId)
                .FirstOrDefaultAsync(cancellationToken);

            if (!isAdmin && !callerPlayerId.HasValue)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

            if (!isAdmin && networkOwnerId.HasValue && callerPlayerId!.Value != networkOwnerId.Value)
            {
                return Results.StatusCode(StatusCodes.Status403Forbidden);
            }

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
            HttpContext context,
            Guid playerId,
            Guid? sectorId,
            IStrategicSystemsService strategicService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var reports = await strategicService.GetIntelligenceReportsAsync(playerId, sectorId, cancellationToken);
            return Results.Ok(reports);
        });

        strategic.MapPost("/intelligence/reports/expire", async (
            HttpContext context,
            IStrategicSystemsService strategicService,
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

            var expired = await strategicService.ExpireIntelligenceReportsAsync(cancellationToken);
            return Results.Ok(new { expired });
        });

        strategic.Map("/ws/dashboard/{playerId:guid}", async (
            HttpContext context,
            Guid playerId,
            int? intervalSeconds,
            IDashboardRealtimeSnapshotService dashboardRealtimeSnapshotService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                await denied.ExecuteAsync(context);
                return;
            }

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
    }

    public static void MapAdminBalanceEndpoints(
        this WebApplication app,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, IConfiguration, CancellationToken, Task<IResult?>> requireAdminBalanceAuthorizationAsync)
    {
        Task<IResult?> RequireAdminBalanceAuthorizationAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            IConfiguration configuration,
            CancellationToken cancellationToken)
            => requireAdminBalanceAuthorizationAsync(context, authService, dbContext, configuration, cancellationToken);

        var adminBalance = app.MapGroup("/api/admin/balance")
            .WithTags("Admin - Balance Controls");

        adminBalance.MapGet("/state", async (
            HttpContext context,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(balanceControlService.GetSnapshot());
        });

        adminBalance.MapPost("/tax", async (
            HttpContext context,
            UpdateTaxRateRequest request,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(balanceControlService.SetTaxRate(request.TaxRatePercent));
        });

        adminBalance.MapPost("/pirates", async (
            HttpContext context,
            UpdatePirateIntensityRequest request,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(balanceControlService.SetPirateIntensity(request.IntensityPercent));
        });

        adminBalance.MapPost("/liquidity", async (
            HttpContext context,
            LiquidityAdjustmentRequest request,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(balanceControlService.ApplyLiquidityAdjustment(request.DeltaPercent, request.Reason ?? "manual"));
        });

        adminBalance.MapPost("/instability", async (
            HttpContext context,
            SectorInstabilityRequest request,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            if (request.SectorId == Guid.Empty)
            {
                return Results.BadRequest(new { error = "sectorId is required." });
            }

            return Results.Ok(balanceControlService.TriggerSectorInstability(request.SectorId, request.Reason ?? "manual"));
        });

        adminBalance.MapPost("/correction", async (
            HttpContext context,
            EconomicCorrectionRequest request,
            IBalanceControlService balanceControlService,
            IConfiguration configuration,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireAdminBalanceAuthorizationAsync(
                context,
                authService,
                dbContext,
                configuration,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            return Results.Ok(balanceControlService.TriggerEconomicCorrection(request.AdjustmentPercent, request.Reason ?? "manual"));
        });

    }

    public static void MapCommunicationEndpoints(
        this WebApplication app,
        ConcurrentDictionary<Guid, (WebSocket Socket, ChannelType ChannelType, string ChannelKey)> channelSockets,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, Guid, CancellationToken, Task<(Guid EffectivePlayerId, bool IsAdmin, IResult? Denied)>> resolveEffectivePlayerIdAsync,
        Func<HttpContext, IAuthService, GalacticTraderDbContext, Guid, CancellationToken, Task<IResult?>> requireOwnerOrAdminAsync)
    {
        Task<(Guid EffectivePlayerId, bool IsAdmin, IResult? Denied)> ResolveEffectivePlayerIdAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            Guid requestedPlayerId,
            CancellationToken cancellationToken)
            => resolveEffectivePlayerIdAsync(context, authService, dbContext, requestedPlayerId, cancellationToken);

        Task<IResult?> RequireOwnerOrAdminAsync(
            HttpContext context,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            Guid ownerPlayerId,
            CancellationToken cancellationToken)
            => requireOwnerOrAdminAsync(context, authService, dbContext, ownerPlayerId, cancellationToken);

        var communication = app.MapGroup("/api/communication")
            .WithTags("Communication");

        communication.MapPost("/subscribe", async (
            HttpContext context,
            SubscribeChannelRequest request,
            ICommunicationService communicationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var result = await communicationService.SubscribeAsync(new SubscribeChannelRequest
            {
                PlayerId = effectivePlayerId,
                ChannelType = request.ChannelType,
                ChannelKey = request.ChannelKey
            }, cancellationToken);
            return Results.Ok(result);
        });

        communication.MapPost("/unsubscribe", async (
            HttpContext context,
            SubscribeChannelRequest request,
            ICommunicationService communicationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var result = await communicationService.UnsubscribeAsync(new SubscribeChannelRequest
            {
                PlayerId = effectivePlayerId,
                ChannelType = request.ChannelType,
                ChannelKey = request.ChannelKey
            }, cancellationToken);
            return Results.Ok(result);
        });

        communication.MapPost("/messages", async (
            HttpContext context,
            SendChannelMessageRequest request,
            ICommunicationService communicationService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            try
            {
                var message = await communicationService.SendMessageAsync(new SendChannelMessageRequest
                {
                    PlayerId = effectivePlayerId,
                    ChannelType = request.ChannelType,
                    ChannelKey = request.ChannelKey,
                    Content = request.Content
                }, cancellationToken);
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
            HttpContext context,
            CreateVoiceChannelRequest request,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.CreatorPlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var channel = await voiceService.CreateChannelAsync(new CreateVoiceChannelRequest
            {
                CreatorPlayerId = effectivePlayerId,
                Mode = request.Mode,
                ScopeKey = request.ScopeKey
            }, cancellationToken);
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
            HttpContext context,
            Guid channelId,
            JoinVoiceChannelRequest request,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var joined = await voiceService.JoinChannelAsync(channelId, new JoinVoiceChannelRequest
            {
                PlayerId = effectivePlayerId
            }, cancellationToken);
            return joined is null ? Results.NotFound() : Results.Ok(joined);
        });

        voice.MapPost("/channels/{channelId:guid}/leave/{playerId:guid}", async (
            HttpContext context,
            Guid channelId,
            Guid playerId,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var left = await voiceService.LeaveChannelAsync(channelId, effectivePlayerId, cancellationToken);
            return left ? Results.NoContent() : Results.NotFound();
        });

        voice.MapPost("/channels/{channelId:guid}/signal", async (
            HttpContext context,
            Guid channelId,
            VoiceSignalRequest request,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectiveSenderId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.SenderId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var signal = await voiceService.PublishSignalAsync(channelId, new VoiceSignalRequest
            {
                SenderId = effectiveSenderId,
                TargetPlayerId = request.TargetPlayerId,
                SignalType = request.SignalType,
                Payload = request.Payload
            }, cancellationToken);
            return signal is null ? Results.NotFound() : Results.Ok(signal);
        });

        voice.MapGet("/channels/{channelId:guid}/signals/{playerId:guid}", async (
            HttpContext context,
            Guid channelId,
            Guid playerId,
            int? limit,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var denied = await RequireOwnerOrAdminAsync(
                context,
                authService,
                dbContext,
                playerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var signals = await voiceService.DequeueSignalsAsync(channelId, playerId, limit ?? 50, cancellationToken);
            return Results.Ok(signals);
        });

        voice.MapPost("/channels/{channelId:guid}/activity", async (
            HttpContext context,
            Guid channelId,
            VoiceActivityRequest request,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.PlayerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var activity = await voiceService.UpdateActivityAsync(channelId, new VoiceActivityRequest
            {
                PlayerId = effectivePlayerId,
                RmsLevel = request.RmsLevel,
                PacketLossPercent = request.PacketLossPercent,
                LatencyMs = request.LatencyMs,
                JitterMs = request.JitterMs
            }, cancellationToken);
            return activity is null ? Results.NotFound() : Results.Ok(activity);
        });

        voice.MapPost("/channels/{channelId:guid}/spatial-audio", async (
            HttpContext context,
            Guid channelId,
            SpatialAudioRequest request,
            IVoiceService voiceService,
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            var (effectiveListenerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                request.ListenerId,
                cancellationToken);
            if (denied is not null)
            {
                return denied;
            }

            var mix = await voiceService.CalculateSpatialMixAsync(channelId, new SpatialAudioRequest
            {
                ListenerId = effectiveListenerId,
                ListenerX = request.ListenerX,
                ListenerY = request.ListenerY,
                ListenerZ = request.ListenerZ,
                FalloffDistance = request.FalloffDistance,
                Speakers = request.Speakers
            }, cancellationToken);
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
            IAuthService authService,
            GalacticTraderDbContext dbContext,
            CancellationToken cancellationToken) =>
        {
            Guid? requestedPlayerId = null;
            if (Guid.TryParse(context.Request.Query["playerId"], out var parsedPlayerId))
            {
                requestedPlayerId = parsedPlayerId;
            }

            var (effectivePlayerId, _, denied) = await ResolveEffectivePlayerIdAsync(
                context,
                authService,
                dbContext,
                requestedPlayerId ?? Guid.Empty,
                cancellationToken);
            if (denied is not null)
            {
                await denied.ExecuteAsync(context);
                return;
            }

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

            await communicationService.SubscribeAsync(new SubscribeChannelRequest
            {
                PlayerId = effectivePlayerId,
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
                            PlayerId = effectivePlayerId,
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
                    PlayerId = effectivePlayerId,
                    ChannelType = parsedChannelType,
                    ChannelKey = normalizedKey
                }, cancellationToken);
            }
        });
    }
}
