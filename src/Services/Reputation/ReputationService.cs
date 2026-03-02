namespace GalacticTrader.Services.Reputation;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class ReputationService : IReputationService
{
    private readonly GalacticTraderDbContext _dbContext;

    public ReputationService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PlayerFactionStandingDto?> AdjustFactionStandingAsync(
        UpdateFactionStandingRequest request,
        CancellationToken cancellationToken = default)
    {
        var playerExists = await _dbContext.Players.AnyAsync(player => player.Id == request.PlayerId, cancellationToken);
        var faction = await _dbContext.Factions.FirstOrDefaultAsync(existing => existing.Id == request.FactionId, cancellationToken);
        if (!playerExists || faction is null)
        {
            return null;
        }

        var relationship = await _dbContext.PlayerFactionRelationships
            .FirstOrDefaultAsync(existing =>
                existing.PlayerId == request.PlayerId &&
                existing.FactionId == request.FactionId,
                cancellationToken);

        if (relationship is null)
        {
            relationship = new PlayerFactionRelationship
            {
                Id = Guid.NewGuid(),
                PlayerId = request.PlayerId,
                FactionId = request.FactionId,
                ReputationScore = 0,
                HasAccess = true,
                TradingDiscount = 0m,
                UpdatedAt = DateTime.UtcNow
            };
            _dbContext.PlayerFactionRelationships.Add(relationship);
        }

        relationship.ReputationScore = Math.Clamp(relationship.ReputationScore + request.ReputationDelta, -100, 100);
        relationship.UpdatedAt = DateTime.UtcNow;
        ApplyThresholds(relationship);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapStanding(relationship, faction.Name);
    }

    public async Task<IReadOnlyList<PlayerFactionStandingDto>> GetFactionStandingsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var relationships = await _dbContext.PlayerFactionRelationships
            .AsNoTracking()
            .Include(relationship => relationship.Faction)
            .Where(relationship => relationship.PlayerId == playerId)
            .OrderByDescending(relationship => relationship.ReputationScore)
            .ToListAsync(cancellationToken);

        return relationships
            .Select(relationship => MapStanding(relationship, relationship.Faction?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<int> ApplyFactionReputationDecayAsync(int points, CancellationToken cancellationToken = default)
    {
        var decay = Math.Max(1, points);
        var relationships = await _dbContext.PlayerFactionRelationships.ToListAsync(cancellationToken);
        foreach (var relationship in relationships)
        {
            if (relationship.ReputationScore > 0)
            {
                relationship.ReputationScore = Math.Max(0, relationship.ReputationScore - decay);
            }
            else if (relationship.ReputationScore < 0)
            {
                relationship.ReputationScore = Math.Min(0, relationship.ReputationScore + decay);
            }

            ApplyThresholds(relationship);
            relationship.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return relationships.Count;
    }

    public async Task<IReadOnlyList<FactionBenefitDto>> GetFactionBenefitsAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var relationships = await _dbContext.PlayerFactionRelationships
            .AsNoTracking()
            .Include(relationship => relationship.Faction)
            .Where(relationship => relationship.PlayerId == playerId)
            .ToListAsync(cancellationToken);

        return relationships
            .Select(relationship =>
            {
                var tier = ResolveTier(relationship.ReputationScore);
                return new FactionBenefitDto
                {
                    FactionId = relationship.FactionId,
                    FactionName = relationship.Faction?.Name ?? "Unknown",
                    Tier = tier,
                    Benefits = ResolveBenefits(relationship.ReputationScore)
                };
            })
            .Where(result => result.Benefits.Count > 0)
            .ToList();
    }

    public async Task<AlignmentStateDto?> ApplyAlignmentActionAsync(
        AlignmentActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var player = await _dbContext.Players.FirstOrDefaultAsync(existing => existing.Id == request.PlayerId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        var baseDelta = request.ActionType switch
        {
            AlignmentActionType.LegalTrade => 3,
            AlignmentActionType.InfrastructureInvestment => 5,
            AlignmentActionType.InsuranceClaim => 2,
            AlignmentActionType.Piracy => -8,
            AlignmentActionType.Smuggling => -6,
            AlignmentActionType.Sabotage => -7,
            AlignmentActionType.SensorSpoofing => -4,
            _ => 0
        };

        var magnitude = Math.Clamp(request.Magnitude, 1, 10);
        player.AlignmentLevel = Math.Clamp(player.AlignmentLevel + (baseDelta * magnitude), -100, 100);

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapAlignmentState(player);
    }

    public async Task<AlignmentAccessDto?> GetAlignmentAccessAsync(Guid playerId, CancellationToken cancellationToken = default)
    {
        var player = await _dbContext.Players
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == playerId, cancellationToken);
        if (player is null)
        {
            return null;
        }

        var path = ResolveAlignmentPath(player.AlignmentLevel);
        return new AlignmentAccessDto
        {
            PlayerId = player.Id,
            AlignmentLevel = player.AlignmentLevel,
            Path = path,
            CanUseLegalInsurance = player.AlignmentLevel > -60,
            CanAccessBlackMarket = player.AlignmentLevel <= -15,
            ScanFrequencyModifier = ResolveScanFrequencyModifier(player.AlignmentLevel),
            InsuranceCostModifier = ResolveInsuranceCostModifier(player.AlignmentLevel)
        };
    }

    private static void ApplyThresholds(PlayerFactionRelationship relationship)
    {
        relationship.HasAccess = relationship.ReputationScore >= -20;
        relationship.TradingDiscount = relationship.ReputationScore switch
        {
            >= 80 => 0.20m,
            >= 40 => 0.10m,
            >= 10 => 0.04m,
            <= -60 => -0.15m,
            <= -30 => -0.08m,
            _ => 0m
        };
    }

    private static string ResolveTier(int reputationScore)
    {
        return reputationScore switch
        {
            >= 80 => "Allied",
            >= 40 => "Friendly",
            >= 0 => "Neutral",
            >= -39 => "Distrusted",
            _ => "Hostile"
        };
    }

    private static IReadOnlyList<string> ResolveBenefits(int reputationScore)
    {
        if (reputationScore >= 80)
        {
            return ["Priority docking", "Tax reduction", "Faction escort support"];
        }

        if (reputationScore >= 40)
        {
            return ["Reduced tariffs", "Faction market access"];
        }

        if (reputationScore <= -60)
        {
            return ["Trade embargo risk", "High inspection frequency"];
        }

        if (reputationScore <= -30)
        {
            return ["Limited docking windows"];
        }

        return [];
    }

    private static PlayerFactionStandingDto MapStanding(PlayerFactionRelationship relationship, string factionName)
    {
        var tier = ResolveTier(relationship.ReputationScore);
        return new PlayerFactionStandingDto
        {
            PlayerId = relationship.PlayerId,
            FactionId = relationship.FactionId,
            ReputationScore = relationship.ReputationScore,
            Tier = tier,
            HasAccess = relationship.HasAccess,
            TradingDiscount = relationship.TradingDiscount,
            TaxModifier = relationship.TradingDiscount >= 0 ? 1m - relationship.TradingDiscount : 1m + Math.Abs(relationship.TradingDiscount),
            Benefits = ResolveBenefits(relationship.ReputationScore)
        };
    }

    private static string ResolveAlignmentPath(int alignmentLevel)
    {
        if (alignmentLevel >= 25)
        {
            return "Lawful";
        }

        if (alignmentLevel <= -25)
        {
            return "Dirty";
        }

        return "Neutral";
    }

    private static float ResolveScanFrequencyModifier(int alignmentLevel)
    {
        return alignmentLevel switch
        {
            >= 50 => 0.75f,
            >= 25 => 0.85f,
            <= -60 => 1.6f,
            <= -25 => 1.3f,
            _ => 1f
        };
    }

    private static float ResolveInsuranceCostModifier(int alignmentLevel)
    {
        return alignmentLevel switch
        {
            >= 50 => 0.80f,
            >= 25 => 0.90f,
            <= -60 => 1.7f,
            <= -25 => 1.35f,
            _ => 1f
        };
    }

    private static AlignmentStateDto MapAlignmentState(Player player)
    {
        var path = ResolveAlignmentPath(player.AlignmentLevel);
        var restrictions = new List<string>();

        if (player.AlignmentLevel <= -60)
        {
            restrictions.Add("Legal insurance denied");
            restrictions.Add("Increased lawful port scans");
        }
        else if (player.AlignmentLevel >= 60)
        {
            restrictions.Add("Black-market contracts blocked");
        }

        return new AlignmentStateDto
        {
            PlayerId = player.Id,
            AlignmentLevel = player.AlignmentLevel,
            Path = path,
            ScanFrequencyModifier = ResolveScanFrequencyModifier(player.AlignmentLevel),
            InsuranceCostModifier = ResolveInsuranceCostModifier(player.AlignmentLevel),
            AccessRestrictions = restrictions
        };
    }

}
