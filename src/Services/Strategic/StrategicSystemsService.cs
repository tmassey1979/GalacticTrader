namespace GalacticTrader.Services.Strategic;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;

public sealed class StrategicSystemsService : IStrategicSystemsService
{
    private readonly GalacticTraderDbContext _dbContext;

    public StrategicSystemsService(GalacticTraderDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<SectorVolatilityCycleDto?> UpsertSectorVolatilityCycleAsync(
        UpdateSectorVolatilityCycleRequest request,
        CancellationToken cancellationToken = default)
    {
        var sector = await _dbContext.Sectors
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == request.SectorId, cancellationToken);
        if (sector is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var cycle = await _dbContext.SectorVolatilityCycles
            .FirstOrDefaultAsync(existing => existing.SectorId == request.SectorId, cancellationToken);

        if (cycle is null)
        {
            cycle = new SectorVolatilityCycle
            {
                Id = Guid.NewGuid(),
                SectorId = request.SectorId,
                CycleStartedAt = now
            };
            _dbContext.SectorVolatilityCycles.Add(cycle);
        }

        cycle.CurrentPhase = string.IsNullOrWhiteSpace(request.CurrentPhase)
            ? "stable"
            : request.CurrentPhase.Trim().ToLowerInvariant();
        cycle.VolatilityIndex = Math.Clamp(request.VolatilityIndex, 0f, 100f);
        cycle.NextTransitionAt = request.NextTransitionAt ?? now.AddHours(6);
        cycle.LastUpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapCycle(cycle, sector.Name);
    }

    public async Task<IReadOnlyList<SectorVolatilityCycleDto>> GetSectorVolatilityCyclesAsync(
        Guid? sectorId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.SectorVolatilityCycles
            .AsNoTracking()
            .Include(cycle => cycle.Sector)
            .AsQueryable();

        if (sectorId.HasValue && sectorId.Value != Guid.Empty)
        {
            query = query.Where(cycle => cycle.SectorId == sectorId.Value);
        }

        var cycles = await query
            .OrderByDescending(cycle => cycle.LastUpdatedAt)
            .ToListAsync(cancellationToken);

        return cycles
            .Select(cycle => MapCycle(cycle, cycle.Sector?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<CorporateWarDto?> DeclareCorporateWarAsync(
        DeclareCorporateWarRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AttackerFactionId == request.DefenderFactionId)
        {
            return null;
        }

        var factions = await _dbContext.Factions
            .Where(faction => faction.Id == request.AttackerFactionId || faction.Id == request.DefenderFactionId)
            .ToDictionaryAsync(faction => faction.Id, cancellationToken);

        if (!factions.TryGetValue(request.AttackerFactionId, out var attacker) ||
            !factions.TryGetValue(request.DefenderFactionId, out var defender))
        {
            return null;
        }

        var activeWar = await _dbContext.CorporateWars
            .FirstOrDefaultAsync(existing =>
                existing.IsActive &&
                ((existing.AttackerFactionId == request.AttackerFactionId && existing.DefenderFactionId == request.DefenderFactionId) ||
                 (existing.AttackerFactionId == request.DefenderFactionId && existing.DefenderFactionId == request.AttackerFactionId)),
                cancellationToken);

        if (activeWar is not null)
        {
            activeWar.Intensity = Math.Clamp(request.Intensity, 1, 100);
            activeWar.CasusBelli = string.IsNullOrWhiteSpace(request.CasusBelli)
                ? "market friction"
                : request.CasusBelli.Trim();
            await _dbContext.SaveChangesAsync(cancellationToken);

            return MapWar(activeWar, attacker.Name, defender.Name);
        }

        var created = new CorporateWar
        {
            Id = Guid.NewGuid(),
            AttackerFactionId = request.AttackerFactionId,
            DefenderFactionId = request.DefenderFactionId,
            CasusBelli = string.IsNullOrWhiteSpace(request.CasusBelli) ? "market friction" : request.CasusBelli.Trim(),
            Intensity = Math.Clamp(request.Intensity, 1, 100),
            StartedAt = DateTime.UtcNow,
            IsActive = true
        };

        _dbContext.CorporateWars.Add(created);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapWar(created, attacker.Name, defender.Name);
    }

    public async Task<IReadOnlyList<CorporateWarDto>> GetCorporateWarsAsync(
        bool activeOnly,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CorporateWars
            .AsNoTracking()
            .Include(war => war.AttackerFaction)
            .Include(war => war.DefenderFaction)
            .AsQueryable();

        if (activeOnly)
        {
            query = query.Where(war => war.IsActive);
        }

        var wars = await query
            .OrderByDescending(war => war.StartedAt)
            .ToListAsync(cancellationToken);

        return wars
            .Select(war => MapWar(
                war,
                war.AttackerFaction?.Name ?? "Unknown",
                war.DefenderFaction?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<InfrastructureOwnershipDto?> UpsertInfrastructureOwnershipAsync(
        UpdateInfrastructureOwnershipRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.InfrastructureType))
        {
            return null;
        }

        var sector = await _dbContext.Sectors
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == request.SectorId, cancellationToken);
        var faction = await _dbContext.Factions
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == request.FactionId, cancellationToken);

        if (sector is null || faction is null)
        {
            return null;
        }

        var normalizedType = request.InfrastructureType.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var ownership = await _dbContext.InfrastructureOwnerships
            .FirstOrDefaultAsync(existing =>
                existing.SectorId == request.SectorId &&
                existing.InfrastructureType == normalizedType,
                cancellationToken);

        if (ownership is null)
        {
            ownership = new InfrastructureOwnership
            {
                Id = Guid.NewGuid(),
                SectorId = request.SectorId,
                InfrastructureType = normalizedType,
                ClaimedAt = now
            };
            _dbContext.InfrastructureOwnerships.Add(ownership);
        }

        ownership.FactionId = request.FactionId;
        ownership.ControlScore = Math.Clamp(request.ControlScore, 0f, 100f);
        ownership.LastUpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapOwnership(ownership, sector.Name, faction.Name);
    }

    public async Task<IReadOnlyList<InfrastructureOwnershipDto>> GetInfrastructureOwnershipAsync(
        Guid? sectorId,
        CancellationToken cancellationToken = default)
    {
        var query = _dbContext.InfrastructureOwnerships
            .AsNoTracking()
            .Include(entry => entry.Sector)
            .Include(entry => entry.Faction)
            .AsQueryable();

        if (sectorId.HasValue && sectorId.Value != Guid.Empty)
        {
            query = query.Where(entry => entry.SectorId == sectorId.Value);
        }

        var entries = await query
            .OrderBy(entry => entry.SectorId)
            .ThenBy(entry => entry.InfrastructureType)
            .ToListAsync(cancellationToken);

        return entries
            .Select(entry => MapOwnership(
                entry,
                entry.Sector?.Name ?? "Unknown",
                entry.Faction?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<TerritoryDominanceDto?> RecalculateTerritoryDominanceAsync(
        Guid factionId,
        CancellationToken cancellationToken = default)
    {
        var faction = await _dbContext.Factions
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == factionId, cancellationToken);
        if (faction is null)
        {
            return null;
        }

        var controlledSectors = await _dbContext.Sectors
            .AsNoTracking()
            .CountAsync(sector => sector.ControlledByFactionId == factionId, cancellationToken);

        var infrastructureScores = await _dbContext.InfrastructureOwnerships
            .AsNoTracking()
            .Where(entry => entry.FactionId == factionId)
            .Select(entry => entry.ControlScore)
            .ToListAsync(cancellationToken);
        var infrastructureScore = infrastructureScores.Count == 0
            ? 0f
            : infrastructureScores.Average();

        var offensiveWarIntensities = await _dbContext.CorporateWars
            .AsNoTracking()
            .Where(war => war.IsActive && war.AttackerFactionId == factionId)
            .Select(war => war.Intensity)
            .ToListAsync(cancellationToken);

        var defensiveWarIntensities = await _dbContext.CorporateWars
            .AsNoTracking()
            .Where(war => war.IsActive && war.DefenderFactionId == factionId)
            .Select(war => war.Intensity)
            .ToListAsync(cancellationToken);

        var offensiveWarPressure = offensiveWarIntensities.Count == 0 ? 0 : offensiveWarIntensities.Average();
        var defensiveWarPressure = defensiveWarIntensities.Count == 0 ? 0 : defensiveWarIntensities.Average();

        var warMomentum = Math.Clamp((float)offensiveWarPressure - (float)defensiveWarPressure, -100f, 100f);
        var dominanceScore = Math.Clamp((controlledSectors * 8f) + (infrastructureScore * 0.6f) + (warMomentum * 0.4f), 0f, 100f);

        var record = await _dbContext.TerritoryDominances
            .FirstOrDefaultAsync(existing => existing.FactionId == factionId, cancellationToken);
        if (record is null)
        {
            record = new TerritoryDominance
            {
                Id = Guid.NewGuid(),
                FactionId = factionId
            };
            _dbContext.TerritoryDominances.Add(record);
        }

        record.ControlledSectorCount = controlledSectors;
        record.InfrastructureControlScore = infrastructureScore;
        record.WarMomentumScore = warMomentum;
        record.DominanceScore = dominanceScore;
        record.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapDominance(record, faction.Name);
    }

    public async Task<IReadOnlyList<TerritoryDominanceDto>> GetTerritoryDominanceAsync(
        CancellationToken cancellationToken = default)
    {
        var entries = await _dbContext.TerritoryDominances
            .AsNoTracking()
            .Include(entry => entry.Faction)
            .OrderByDescending(entry => entry.DominanceScore)
            .ToListAsync(cancellationToken);

        return entries
            .Select(entry => MapDominance(entry, entry.Faction?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<InsurancePolicyDto?> UpsertInsurancePolicyAsync(
        UpsertInsurancePolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var ship = await _dbContext.Ships
            .AsNoTracking()
            .FirstOrDefaultAsync(existing =>
                existing.Id == request.ShipId &&
                existing.PlayerId == request.PlayerId,
                cancellationToken);
        if (ship is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var policy = await _dbContext.InsurancePolicies
            .FirstOrDefaultAsync(existing => existing.ShipId == request.ShipId, cancellationToken);

        if (policy is null)
        {
            policy = new InsurancePolicy
            {
                Id = Guid.NewGuid(),
                PlayerId = request.PlayerId,
                ShipId = request.ShipId,
                CreatedAt = now
            };
            _dbContext.InsurancePolicies.Add(policy);
        }

        policy.CoverageRate = Math.Clamp(request.CoverageRate, 0.1f, 0.95f);
        policy.PremiumPerCycle = Math.Max(0m, request.PremiumPerCycle);
        policy.RiskTier = string.IsNullOrWhiteSpace(request.RiskTier)
            ? "standard"
            : request.RiskTier.Trim().ToLowerInvariant();
        policy.IsActive = request.IsActive;
        policy.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapInsurancePolicy(policy, ship.Name);
    }

    public async Task<IReadOnlyList<InsurancePolicyDto>> GetInsurancePoliciesAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var policies = await _dbContext.InsurancePolicies
            .AsNoTracking()
            .Include(policy => policy.Ship)
            .Where(policy => policy.PlayerId == playerId)
            .OrderByDescending(policy => policy.UpdatedAt)
            .ToListAsync(cancellationToken);

        return policies
            .Select(policy => MapInsurancePolicy(policy, policy.Ship?.Name ?? "Unknown"))
            .ToList();
    }

    public async Task<InsuranceClaimDto?> FileInsuranceClaimAsync(
        FileInsuranceClaimRequest request,
        CancellationToken cancellationToken = default)
    {
        var policy = await _dbContext.InsurancePolicies
            .Include(existing => existing.Ship)
            .FirstOrDefaultAsync(existing => existing.Id == request.PolicyId, cancellationToken);
        if (policy is null || !policy.IsActive || policy.Ship is null)
        {
            return null;
        }

        if (request.ClaimAmount <= 0)
        {
            return null;
        }

        var combatLogExists = request.CombatLogId is null || await _dbContext.CombatLogs
            .AsNoTracking()
            .AnyAsync(log => log.Id == request.CombatLogId.Value, cancellationToken);
        if (!combatLogExists)
        {
            return null;
        }

        var maxCoveredAmount = policy.Ship.CurrentValue * (decimal)policy.CoverageRate;
        var tierRisk = policy.RiskTier switch
        {
            "high" => 60f,
            "medium" => 40f,
            "low" => 20f,
            _ => 30f
        };

        var fraudRisk = tierRisk;
        if (request.CombatLogId is null)
        {
            fraudRisk += 20f;
        }

        if (request.ClaimAmount > (maxCoveredAmount * 1.10m))
        {
            fraudRisk += 15f;
        }

        fraudRisk = Math.Clamp(fraudRisk, 0f, 100f);
        var status = fraudRisk >= 75f ? "rejected" : "approved";
        var now = DateTime.UtcNow;
        var claim = new InsuranceClaim
        {
            Id = Guid.NewGuid(),
            PolicyId = policy.Id,
            CombatLogId = request.CombatLogId,
            ClaimAmount = Math.Min(request.ClaimAmount, maxCoveredAmount),
            FraudRiskScore = fraudRisk,
            Status = status,
            FiledAt = now,
            ResolvedAt = now
        };

        _dbContext.InsuranceClaims.Add(claim);

        if (status == "approved")
        {
            var player = await _dbContext.Players
                .FirstOrDefaultAsync(existing => existing.Id == policy.PlayerId, cancellationToken);
            if (player is not null)
            {
                player.LiquidCredits += claim.ClaimAmount;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapInsuranceClaim(claim, policy.PlayerId, policy.ShipId);
    }

    public async Task<IReadOnlyList<InsuranceClaimDto>> GetInsuranceClaimsAsync(
        Guid playerId,
        CancellationToken cancellationToken = default)
    {
        var claims = await _dbContext.InsuranceClaims
            .AsNoTracking()
            .Include(claim => claim.Policy)
            .Where(claim => claim.Policy != null && claim.Policy.PlayerId == playerId)
            .OrderByDescending(claim => claim.FiledAt)
            .ToListAsync(cancellationToken);

        return claims
            .Select(claim => MapInsuranceClaim(
                claim,
                claim.Policy?.PlayerId ?? Guid.Empty,
                claim.Policy?.ShipId ?? Guid.Empty))
            .ToList();
    }

    public async Task<IntelligenceNetworkDto?> CreateIntelligenceNetworkAsync(
        CreateIntelligenceNetworkRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return null;
        }

        var playerExists = await _dbContext.Players
            .AsNoTracking()
            .AnyAsync(player => player.Id == request.OwnerPlayerId, cancellationToken);
        if (!playerExists)
        {
            return null;
        }

        var normalizedName = request.Name.Trim().ToLowerInvariant();
        var now = DateTime.UtcNow;
        var existing = await _dbContext.IntelligenceNetworks
            .FirstOrDefaultAsync(network =>
                network.OwnerPlayerId == request.OwnerPlayerId &&
                network.Name == normalizedName,
                cancellationToken);

        if (existing is null)
        {
            existing = new IntelligenceNetwork
            {
                Id = Guid.NewGuid(),
                OwnerPlayerId = request.OwnerPlayerId,
                Name = normalizedName,
                CreatedAt = now
            };
            _dbContext.IntelligenceNetworks.Add(existing);
        }

        existing.AssetCount = Math.Clamp(request.AssetCount, 1, 500);
        existing.CoverageScore = Math.Clamp(request.CoverageScore, 0f, 100f);
        existing.IsActive = true;
        existing.UpdatedAt = now;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapIntelligenceNetwork(existing);
    }

    public async Task<IntelligenceReportDto?> PublishIntelligenceReportAsync(
        PublishIntelligenceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.SignalType) || string.IsNullOrWhiteSpace(request.Payload))
        {
            return null;
        }

        var network = await _dbContext.IntelligenceNetworks
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == request.NetworkId && existing.IsActive, cancellationToken);
        var sector = await _dbContext.Sectors
            .AsNoTracking()
            .FirstOrDefaultAsync(existing => existing.Id == request.SectorId, cancellationToken);

        if (network is null || sector is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var ttlMinutes = Math.Clamp(request.TtlMinutes, 5, 1440);
        var report = new IntelligenceReport
        {
            Id = Guid.NewGuid(),
            NetworkId = request.NetworkId,
            SectorId = request.SectorId,
            SignalType = request.SignalType.Trim().ToLowerInvariant(),
            ConfidenceScore = Math.Clamp(request.ConfidenceScore, 0f, 100f),
            Payload = request.Payload.Trim(),
            DetectedAt = now,
            ExpiresAt = now.AddMinutes(ttlMinutes),
            IsExpired = false
        };

        _dbContext.IntelligenceReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapIntelligenceReport(report, network.Name, sector.Name);
    }

    public async Task<IReadOnlyList<IntelligenceReportDto>> GetIntelligenceReportsAsync(
        Guid playerId,
        Guid? sectorId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var query = _dbContext.IntelligenceReports
            .AsNoTracking()
            .Include(report => report.Network)
            .Include(report => report.Sector)
            .Where(report =>
                report.Network != null &&
                report.Network.OwnerPlayerId == playerId &&
                !report.IsExpired &&
                report.ExpiresAt > now)
            .AsQueryable();

        if (sectorId.HasValue && sectorId.Value != Guid.Empty)
        {
            query = query.Where(report => report.SectorId == sectorId.Value);
        }

        var reports = await query
            .OrderByDescending(report => report.ConfidenceScore)
            .ThenByDescending(report => report.DetectedAt)
            .ToListAsync(cancellationToken);

        return reports
            .Select(report => MapIntelligenceReport(
                report,
                report.Network?.Name ?? "unknown",
                report.Sector?.Name ?? "unknown"))
            .ToList();
    }

    public async Task<int> ExpireIntelligenceReportsAsync(
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var stale = await _dbContext.IntelligenceReports
            .Where(report => !report.IsExpired && report.ExpiresAt <= now)
            .ToListAsync(cancellationToken);

        foreach (var report in stale)
        {
            report.IsExpired = true;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return stale.Count;
    }

    private static SectorVolatilityCycleDto MapCycle(SectorVolatilityCycle cycle, string sectorName)
    {
        return new SectorVolatilityCycleDto
        {
            Id = cycle.Id,
            SectorId = cycle.SectorId,
            SectorName = sectorName,
            CurrentPhase = cycle.CurrentPhase,
            VolatilityIndex = cycle.VolatilityIndex,
            CycleStartedAt = cycle.CycleStartedAt,
            NextTransitionAt = cycle.NextTransitionAt,
            LastUpdatedAt = cycle.LastUpdatedAt
        };
    }

    private static CorporateWarDto MapWar(CorporateWar war, string attackerName, string defenderName)
    {
        return new CorporateWarDto
        {
            Id = war.Id,
            AttackerFactionId = war.AttackerFactionId,
            AttackerFactionName = attackerName,
            DefenderFactionId = war.DefenderFactionId,
            DefenderFactionName = defenderName,
            CasusBelli = war.CasusBelli,
            Intensity = war.Intensity,
            StartedAt = war.StartedAt,
            EndedAt = war.EndedAt,
            IsActive = war.IsActive
        };
    }

    private static InfrastructureOwnershipDto MapOwnership(
        InfrastructureOwnership ownership,
        string sectorName,
        string factionName)
    {
        return new InfrastructureOwnershipDto
        {
            Id = ownership.Id,
            SectorId = ownership.SectorId,
            SectorName = sectorName,
            FactionId = ownership.FactionId,
            FactionName = factionName,
            InfrastructureType = ownership.InfrastructureType,
            ControlScore = ownership.ControlScore,
            ClaimedAt = ownership.ClaimedAt,
            LastUpdatedAt = ownership.LastUpdatedAt
        };
    }

    private static TerritoryDominanceDto MapDominance(TerritoryDominance record, string factionName)
    {
        return new TerritoryDominanceDto
        {
            Id = record.Id,
            FactionId = record.FactionId,
            FactionName = factionName,
            ControlledSectorCount = record.ControlledSectorCount,
            InfrastructureControlScore = record.InfrastructureControlScore,
            WarMomentumScore = record.WarMomentumScore,
            DominanceScore = record.DominanceScore,
            UpdatedAt = record.UpdatedAt
        };
    }

    private static InsurancePolicyDto MapInsurancePolicy(InsurancePolicy policy, string shipName)
    {
        return new InsurancePolicyDto
        {
            Id = policy.Id,
            PlayerId = policy.PlayerId,
            ShipId = policy.ShipId,
            ShipName = shipName,
            CoverageRate = policy.CoverageRate,
            PremiumPerCycle = policy.PremiumPerCycle,
            RiskTier = policy.RiskTier,
            IsActive = policy.IsActive,
            LastPremiumChargedAt = policy.LastPremiumChargedAt,
            UpdatedAt = policy.UpdatedAt
        };
    }

    private static InsuranceClaimDto MapInsuranceClaim(InsuranceClaim claim, Guid playerId, Guid shipId)
    {
        return new InsuranceClaimDto
        {
            Id = claim.Id,
            PolicyId = claim.PolicyId,
            PlayerId = playerId,
            ShipId = shipId,
            ClaimAmount = claim.ClaimAmount,
            FraudRiskScore = claim.FraudRiskScore,
            Status = claim.Status,
            FiledAt = claim.FiledAt,
            ResolvedAt = claim.ResolvedAt
        };
    }

    private static IntelligenceNetworkDto MapIntelligenceNetwork(IntelligenceNetwork network)
    {
        return new IntelligenceNetworkDto
        {
            Id = network.Id,
            OwnerPlayerId = network.OwnerPlayerId,
            Name = network.Name,
            AssetCount = network.AssetCount,
            CoverageScore = network.CoverageScore,
            IsActive = network.IsActive,
            UpdatedAt = network.UpdatedAt
        };
    }

    private static IntelligenceReportDto MapIntelligenceReport(
        IntelligenceReport report,
        string networkName,
        string sectorName)
    {
        return new IntelligenceReportDto
        {
            Id = report.Id,
            NetworkId = report.NetworkId,
            NetworkName = networkName,
            SectorId = report.SectorId,
            SectorName = sectorName,
            SignalType = report.SignalType,
            ConfidenceScore = report.ConfidenceScore,
            Payload = report.Payload,
            DetectedAt = report.DetectedAt,
            ExpiresAt = report.ExpiresAt,
            IsExpired = report.IsExpired
        };
    }
}
