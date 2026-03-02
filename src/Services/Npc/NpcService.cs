namespace GalacticTrader.Services.Npc;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Data.Repositories.Navigation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class NpcService : INpcService
{
    private static readonly IReadOnlyDictionary<NpcArchetype, ArchetypeProfile> ArchetypeProfiles =
        new Dictionary<NpcArchetype, ArchetypeProfile>
        {
            [NpcArchetype.Merchant] = new(StartingWealth: 200_000m, RiskTolerance: 25, AggressionIndex: -30, TradeWeight: 0.85f, RaidWeight: 0.05f, ExpandWeight: 0.20f),
            [NpcArchetype.Industrialist] = new(StartingWealth: 500_000m, RiskTolerance: 15, AggressionIndex: -40, TradeWeight: 0.90f, RaidWeight: 0.02f, ExpandWeight: 0.35f),
            [NpcArchetype.ReputableTrader] = new(StartingWealth: 300_000m, RiskTolerance: 20, AggressionIndex: -20, TradeWeight: 0.88f, RaidWeight: 0.03f, ExpandWeight: 0.25f),
            [NpcArchetype.RogueTrader] = new(StartingWealth: 250_000m, RiskTolerance: 55, AggressionIndex: 10, TradeWeight: 0.60f, RaidWeight: 0.25f, ExpandWeight: 0.30f),
            [NpcArchetype.Pirate] = new(StartingWealth: 120_000m, RiskTolerance: 80, AggressionIndex: 75, TradeWeight: 0.15f, RaidWeight: 0.90f, ExpandWeight: 0.20f),
            [NpcArchetype.AlienSyndicate] = new(StartingWealth: 700_000m, RiskTolerance: 70, AggressionIndex: 45, TradeWeight: 0.55f, RaidWeight: 0.45f, ExpandWeight: 0.55f)
        };

    private readonly GalacticTraderDbContext _dbContext;
    private readonly IRouteRepository _routeRepository;
    private readonly ILogger<NpcService> _logger;

    public NpcService(
        GalacticTraderDbContext dbContext,
        IRouteRepository routeRepository,
        ILogger<NpcService> logger)
    {
        _dbContext = dbContext;
        _routeRepository = routeRepository;
        _logger = logger;
    }

    public async Task<NpcAgentDto> CreateAgentAsync(CreateNpcRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Agent name is required.");
        }

        var profile = ArchetypeProfiles[request.Archetype];

        var agent = new NPCAgent
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Archetype = request.Archetype.ToString(),
            FactionId = request.FactionId,
            WealthTarget = (float)profile.StartingWealth * 2f,
            RiskTolerance = profile.RiskTolerance,
            AggressionIndex = profile.AggressionIndex,
            Wealth = profile.StartingWealth,
            ReputationScore = 0,
            InfluenceScore = 0f,
            FleetSize = 0,
            CurrentLocationId = request.StartingSectorId,
            TargetLocationId = request.StartingSectorId,
            CurrentGoal = "EstablishOperations",
            DecisionTick = 0,
            TradeVolume24h = 0m,
            TradesLegally = request.Archetype is not (NpcArchetype.Pirate or NpcArchetype.RogueTrader),
            TradesIllegally = request.Archetype is NpcArchetype.Pirate or NpcArchetype.RogueTrader or NpcArchetype.AlienSyndicate
        };

        _dbContext.NPCAgents.Add(agent);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapAgent(agent);
    }

    public async Task<IReadOnlyList<NpcAgentDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.NPCAgents
            .AsNoTracking()
            .OrderBy(agent => agent.Name)
            .ToListAsync(cancellationToken);

        return agents.Select(MapAgent).ToList();
    }

    public async Task<NpcAgentDto?> GetAgentAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        return agent is null ? null : MapAgent(agent);
    }

    public async Task<NpcDecisionResult?> ProcessDecisionTickAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents.FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var previousGoal = agent.CurrentGoal;
        var previousTarget = agent.TargetLocationId;
        var profile = ArchetypeProfiles[ParseArchetype(agent.Archetype)];

        var opportunity = await ScanOpportunityAsync(agent, cancellationToken);
        var threatScore = await GetThreatScoreAsync(agent.CurrentLocationId, cancellationToken);
        var expandScore = profile.ExpandWeight + (agent.FleetSize < 3 ? 0.25f : 0f);
        var tradeScore = profile.TradeWeight + (float)Math.Clamp((double)(opportunity / 100m), 0d, 0.4d);
        var raidScore = profile.RaidWeight + (threatScore * 0.15f) + (agent.AggressionIndex > 0 ? 0.2f : 0f);
        var patrolScore = 0.2f + (threatScore * 0.2f);

        var selectedGoal = new[]
        {
            ("Trade", tradeScore),
            ("Raid", raidScore),
            ("ExpandFleet", expandScore),
            ("Patrol", patrolScore)
        }.OrderByDescending(item => item.Item2).First().Item1;

        agent.CurrentGoal = selectedGoal;
        agent.DecisionTick += 1;

        if (selectedGoal is "Trade" or "Raid")
        {
            agent.TargetLocationId = await SelectTargetSectorAsync(agent.CurrentLocationId, cancellationToken);
        }
        else if (selectedGoal == "Patrol")
        {
            agent.TargetLocationId = agent.CurrentLocationId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new NpcDecisionResult
        {
            AgentId = agent.Id,
            DecisionTick = agent.DecisionTick,
            PreviousGoal = previousGoal,
            CurrentGoal = agent.CurrentGoal,
            PreviousTarget = previousTarget,
            CurrentTarget = agent.TargetLocationId,
            OpportunityScore = (float)opportunity
        };
    }

    public async Task<IReadOnlyList<NpcDecisionResult>> ProcessAllDecisionTicksAsync(CancellationToken cancellationToken = default)
    {
        var agents = await _dbContext.NPCAgents
            .AsNoTracking()
            .Select(agent => agent.Id)
            .ToListAsync(cancellationToken);

        var results = new List<NpcDecisionResult>(agents.Count);
        foreach (var agentId in agents)
        {
            var result = await ProcessDecisionTickAsync(agentId, cancellationToken);
            if (result is not null)
            {
                results.Add(result);
            }
        }

        return results;
    }

    public async Task<NpcFleetSummary?> SpawnFleetAsync(Guid agentId, int shipCount, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var existingShips = await _dbContext.NPCShips
            .AsNoTracking()
            .Where(ship => ship.NPCAgentId == agent.Id)
            .ToListAsync(cancellationToken);

        var count = Math.Clamp(shipCount, 1, 20);
        var archetype = ParseArchetype(agent.Archetype);
        var newShips = new List<NPCShip>(count);

        for (var index = 0; index < count; index++)
        {
            var shipClass = ResolveFleetComposition(archetype, index);
            newShips.Add(new NPCShip
            {
                Id = Guid.NewGuid(),
                NPCAgentId = agent.Id,
                Name = $"{agent.Name}-{shipClass}-{index + 1}",
                ShipClass = shipClass,
                HullIntegrity = 100 + (shipClass == "Battleship" ? 120 : 0),
                MaxHullIntegrity = 220,
                CombatRating = shipClass switch
                {
                    "Battleship" => 85,
                    "Escort" => 60,
                    "Trader" => 45,
                    _ => 55
                },
                CurrentSectorId = agent.CurrentLocationId,
                IsActive = true
            });
        }

        _dbContext.NPCShips.AddRange(newShips);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var summaryProjection = new NPCAgent
        {
            Id = agent.Id,
            FleetSize = existingShips.Count + newShips.Count,
            AggressionIndex = agent.AggressionIndex,
            Ships = existingShips.Concat(newShips).ToList()
        };

        return MapFleetSummary(summaryProjection);
    }

    public async Task<bool> PlanRouteAsync(Guid agentId, Guid targetSectorId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents.FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return false;
        }

        var targetExists = await _dbContext.Sectors.AnyAsync(sector => sector.Id == targetSectorId, cancellationToken);
        if (!targetExists)
        {
            return false;
        }

        var canReach = await HasRoutePathAsync(agent.CurrentLocationId, targetSectorId, cancellationToken);
        if (!canReach)
        {
            return false;
        }

        agent.TargetLocationId = targetSectorId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> ProcessFleetMovementAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents
            .Include(existing => existing.Ships)
            .FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        if (agent is null || !agent.TargetLocationId.HasValue || !agent.CurrentLocationId.HasValue)
        {
            return false;
        }

        if (agent.CurrentLocationId == agent.TargetLocationId)
        {
            return true;
        }

        var nextSectorId = await ResolveNextSectorAsync(agent.CurrentLocationId.Value, agent.TargetLocationId.Value, cancellationToken);
        if (!nextSectorId.HasValue)
        {
            return false;
        }

        foreach (var ship in agent.Ships.Where(ship => ship.IsActive))
        {
            ship.CurrentSectorId = nextSectorId.Value;
        }

        agent.CurrentLocationId = nextSectorId.Value;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<decimal?> ExecuteNpcTradeAsync(Guid agentId, CancellationToken cancellationToken = default)
    {
        var agent = await _dbContext.NPCAgents.FirstOrDefaultAsync(existing => existing.Id == agentId, cancellationToken);
        if (agent is null)
        {
            return null;
        }

        var listing = await _dbContext.MarketListings
            .OrderByDescending(marketListing => marketListing.DemandMultiplier * marketListing.ScarcityModifier)
            .FirstOrDefaultAsync(cancellationToken);

        if (listing is null)
        {
            return null;
        }

        var volume = Math.Max(1, listing.AvailableQuantity / 100);
        var gross = listing.CurrentPrice * volume;
        var margin = gross * 0.08m;

        agent.Wealth += margin;
        agent.TradeVolume24h += gross;
        agent.ReputationScore += agent.TradesLegally ? 1 : -1;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return margin;
    }

    private async Task<decimal> ScanOpportunityAsync(NPCAgent agent, CancellationToken cancellationToken)
    {
        var listing = await _dbContext.MarketListings
            .OrderByDescending(marketListing => marketListing.CurrentPrice * (decimal)(marketListing.DemandMultiplier * marketListing.ScarcityModifier))
            .FirstOrDefaultAsync(cancellationToken);
        if (listing is null)
        {
            return 0m;
        }

        var multiplier = (decimal)(listing.DemandMultiplier * listing.ScarcityModifier);
        return decimal.Round(listing.CurrentPrice * multiplier, 2);
    }

    private async Task<float> GetThreatScoreAsync(Guid? sectorId, CancellationToken cancellationToken)
    {
        if (!sectorId.HasValue)
        {
            return 0f;
        }

        var sector = await _dbContext.Sectors
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == sectorId.Value, cancellationToken);
        if (sector is null)
        {
            return 0f;
        }

        return Math.Clamp((sector.HazardRating + sector.PiratePresenceProbability) / 200f, 0f, 1f);
    }

    private async Task<Guid?> SelectTargetSectorAsync(Guid? currentSectorId, CancellationToken cancellationToken)
    {
        var candidates = await _dbContext.Sectors
            .AsNoTracking()
            .OrderByDescending(sector => sector.EconomicIndex - sector.HazardRating)
            .Select(sector => sector.Id)
            .ToListAsync(cancellationToken);

        return candidates.FirstOrDefault(id => id != currentSectorId);
    }

    private async Task<bool> HasRoutePathAsync(Guid? fromSectorId, Guid toSectorId, CancellationToken cancellationToken)
    {
        if (!fromSectorId.HasValue)
        {
            return false;
        }

        if (fromSectorId.Value == toSectorId)
        {
            return true;
        }

        var routes = await _routeRepository.GetAllAsync(cancellationToken);
        var adjacency = routes
            .GroupBy(route => route.FromSectorId)
            .ToDictionary(group => group.Key, group => group.Select(route => route.ToSectorId).ToList());

        var visited = new HashSet<Guid> { fromSectorId.Value };
        var queue = new Queue<Guid>();
        queue.Enqueue(fromSectorId.Value);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (!adjacency.TryGetValue(current, out var nextNodes))
            {
                continue;
            }

            foreach (var next in nextNodes)
            {
                if (!visited.Add(next))
                {
                    continue;
                }

                if (next == toSectorId)
                {
                    return true;
                }

                queue.Enqueue(next);
            }
        }

        return false;
    }

    private async Task<Guid?> ResolveNextSectorAsync(Guid fromSectorId, Guid toSectorId, CancellationToken cancellationToken)
    {
        var routes = await _routeRepository.GetOutboundAsync(fromSectorId, cancellationToken);
        var direct = routes.FirstOrDefault(route => route.ToSectorId == toSectorId);
        if (direct is not null)
        {
            return direct.ToSectorId;
        }

        return routes
            .OrderBy(route => route.BaseRiskScore)
            .Select(route => (Guid?)route.ToSectorId)
            .FirstOrDefault();
    }

    private static string ResolveFleetComposition(NpcArchetype archetype, int index)
    {
        return archetype switch
        {
            NpcArchetype.Merchant or NpcArchetype.ReputableTrader => index % 3 == 0 ? "Escort" : "Trader",
            NpcArchetype.Industrialist => index % 4 == 0 ? "Battleship" : "Hauler",
            NpcArchetype.RogueTrader => index % 2 == 0 ? "Raider" : "Trader",
            NpcArchetype.Pirate => index % 3 == 0 ? "Battleship" : "Raider",
            NpcArchetype.AlienSyndicate => index % 2 == 0 ? "Battleship" : "Escort",
            _ => "Trader"
        };
    }

    private static NpcArchetype ParseArchetype(string archetype)
    {
        return Enum.TryParse<NpcArchetype>(archetype, out var parsed)
            ? parsed
            : NpcArchetype.Merchant;
    }

    private static NpcAgentDto MapAgent(NPCAgent agent)
    {
        return new NpcAgentDto
        {
            Id = agent.Id,
            Name = agent.Name,
            Archetype = agent.Archetype,
            Wealth = agent.Wealth,
            ReputationScore = agent.ReputationScore,
            FleetSize = agent.FleetSize,
            RiskTolerance = agent.RiskTolerance,
            InfluenceScore = agent.InfluenceScore,
            CurrentGoal = agent.CurrentGoal,
            CurrentLocationId = agent.CurrentLocationId,
            TargetLocationId = agent.TargetLocationId,
            DecisionTick = agent.DecisionTick
        };
    }

    private static NpcFleetSummary MapFleetSummary(NPCAgent agent)
    {
        var activeShips = agent.Ships.Count(ship => ship.IsActive);
        var coordinationBonus = Math.Clamp(activeShips / 10f + (agent.AggressionIndex / 200f), 0f, 1f);

        return new NpcFleetSummary
        {
            AgentId = agent.Id,
            FleetSize = agent.FleetSize,
            ActiveShips = activeShips,
            CoordinationBonus = coordinationBonus,
            Ships = agent.Ships.Select(ship => new NpcShipDto
            {
                Id = ship.Id,
                Name = ship.Name,
                ShipClass = ship.ShipClass,
                HullIntegrity = ship.HullIntegrity,
                CombatRating = ship.CombatRating,
                CurrentSectorId = ship.CurrentSectorId,
                IsActive = ship.IsActive
            }).ToList()
        };
    }

    private readonly record struct ArchetypeProfile(
        decimal StartingWealth,
        float RiskTolerance,
        int AggressionIndex,
        float TradeWeight,
        float RaidWeight,
        float ExpandWeight);
}
