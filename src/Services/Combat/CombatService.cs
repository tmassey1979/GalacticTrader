namespace GalacticTrader.Services.Combat;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class CombatService : ICombatService
{
    public const int TickDurationMilliseconds = 250;

    private static readonly SubsystemType[] TargetOrder =
    [
        SubsystemType.Shields,
        SubsystemType.Weapons,
        SubsystemType.Engines,
        SubsystemType.Sensors,
        SubsystemType.Reactor,
        SubsystemType.Cargo,
        SubsystemType.LifeSupport,
        SubsystemType.Hull
    ];

    private static readonly Lock LockObj = new();
    private static readonly Dictionary<Guid, CombatInternalState> ActiveCombats = [];

    private readonly GalacticTraderDbContext _dbContext;
    private readonly ILogger<CombatService> _logger;

    public CombatService(GalacticTraderDbContext dbContext, ILogger<CombatService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<CombatSummaryDto> StartCombatAsync(
        StartCombatRequest request,
        CancellationToken cancellationToken = default)
    {
        var attackerShip = await _dbContext.Ships
            .Include(ship => ship.Player)
            .Include(ship => ship.Crew)
            .Include(ship => ship.Modules)
            .FirstOrDefaultAsync(ship => ship.Id == request.AttackerShipId, cancellationToken);
        var defenderShip = await _dbContext.Ships
            .Include(ship => ship.Player)
            .Include(ship => ship.Crew)
            .Include(ship => ship.Modules)
            .FirstOrDefaultAsync(ship => ship.Id == request.DefenderShipId, cancellationToken);

        if (attackerShip is null || defenderShip is null)
        {
            throw new InvalidOperationException("Attacker and defender ships are required.");
        }

        var combat = new CombatInternalState
        {
            CombatId = Guid.NewGuid(),
            State = CombatState.Active,
            StartedAtUtc = DateTime.UtcNow,
            MaxTicks = Math.Clamp(request.MaxTicks, 1, 10_000),
            TickCount = 0,
            Attacker = CreateCombatantState(attackerShip),
            Defender = CreateCombatantState(defenderShip)
        };

        lock (LockObj)
        {
            ActiveCombats[combat.CombatId] = combat;
        }

        _logger.LogInformation(
            "Combat started. CombatId={CombatId}, AttackerShipId={AttackerShipId}, DefenderShipId={DefenderShipId}",
            combat.CombatId,
            request.AttackerShipId,
            request.DefenderShipId);

        return MapSummary(combat);
    }

    public Task<CombatSummaryDto?> GetCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            return Task.FromResult(
                ActiveCombats.TryGetValue(combatId, out var combat)
                    ? MapSummary(combat)
                    : null);
        }
    }

    public Task<IReadOnlyList<CombatSummaryDto>> GetActiveCombatsAsync(CancellationToken cancellationToken = default)
    {
        lock (LockObj)
        {
            var list = ActiveCombats.Values
                .Where(combat => combat.State == CombatState.Active)
                .Select(MapSummary)
                .ToList();
            return Task.FromResult((IReadOnlyList<CombatSummaryDto>)list);
        }
    }

    public async Task<CombatTickResultDto?> ProcessTickAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        CombatInternalState? combat;
        lock (LockObj)
        {
            ActiveCombats.TryGetValue(combatId, out combat);
        }

        if (combat is null)
        {
            return null;
        }

        return await ProcessSingleTickInternalAsync(combat, cancellationToken);
    }

    public async Task<IReadOnlyList<CombatTickResultDto>> ProcessTicksAsync(
        Guid combatId,
        int tickCount,
        CancellationToken cancellationToken = default)
    {
        var results = new List<CombatTickResultDto>();
        var count = Math.Clamp(tickCount, 1, 2_000);
        for (var index = 0; index < count; index++)
        {
            var result = await ProcessTickAsync(combatId, cancellationToken);
            if (result is null)
            {
                break;
            }

            results.Add(result);
            if (result.State != CombatState.Active)
            {
                break;
            }
        }

        return results;
    }

    public async Task<CombatSummaryDto?> EndCombatAsync(Guid combatId, CancellationToken cancellationToken = default)
    {
        CombatInternalState? combat;
        lock (LockObj)
        {
            ActiveCombats.TryGetValue(combatId, out combat);
        }

        if (combat is null)
        {
            return null;
        }

        if (combat.State == CombatState.Active)
        {
            combat.State = CombatState.Completed;
            combat.EndedAtUtc = DateTime.UtcNow;
            await PersistCombatLogAndEffectsAsync(combat, cancellationToken);
        }

        lock (LockObj)
        {
            ActiveCombats.Remove(combatId);
        }

        return MapSummary(combat);
    }

    public async Task<IReadOnlyList<CombatLogDto>> GetRecentCombatLogsAsync(
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var takeCount = Math.Clamp(limit, 1, 500);
        var logs = await _dbContext.CombatLogs
            .AsNoTracking()
            .OrderByDescending(log => log.BattleStartedAt)
            .Take(takeCount)
            .ToListAsync(cancellationToken);

        return logs.Select(log => new CombatLogDto
        {
            Id = log.Id,
            AttackerId = log.AttackerId,
            DefenderId = log.DefenderId,
            AttackerShipId = log.AttackerShipId,
            DefenderShipId = log.DefenderShipId,
            BattleOutcome = log.BattleOutcome,
            BattleStartedAt = log.BattleStartedAt,
            BattleEndedAt = log.BattleEndedAt,
            DurationSeconds = log.DurationSeconds,
            TotalTicks = log.TotalTicks,
            InsurancePayout = log.InsurancePayout
        }).ToList();
    }

    private async Task<CombatTickResultDto> ProcessSingleTickInternalAsync(
        CombatInternalState combat,
        CancellationToken cancellationToken)
    {
        if (combat.State != CombatState.Active)
        {
            return MapTickResult(combat, []);
        }

        var hits = new List<SubsystemHitDto>(2);
        combat.TickCount++;

        if (combat.Attacker.HullCurrent > 0 && combat.Defender.HullCurrent > 0)
        {
            var hit = ExecuteAttack(combat, combat.Attacker, combat.Defender);
            if (hit is not null)
            {
                hits.Add(hit);
            }
        }

        if (combat.Defender.HullCurrent > 0 && combat.Attacker.HullCurrent > 0)
        {
            var retaliation = ExecuteAttack(combat, combat.Defender, combat.Attacker);
            if (retaliation is not null)
            {
                hits.Add(retaliation);
            }
        }

        RegenerateShields(combat.Attacker);
        RegenerateShields(combat.Defender);

        if (combat.Attacker.HullCurrent <= 0 ||
            combat.Defender.HullCurrent <= 0 ||
            combat.TickCount >= combat.MaxTicks)
        {
            combat.State = CombatState.Completed;
            combat.EndedAtUtc = DateTime.UtcNow;
            await PersistCombatLogAndEffectsAsync(combat, cancellationToken);
            lock (LockObj)
            {
                ActiveCombats.Remove(combat.CombatId);
            }
        }

        return MapTickResult(combat, hits);
    }

    private SubsystemHitDto? ExecuteAttack(
        CombatInternalState combat,
        CombatantState attacker,
        CombatantState defender)
    {
        var targetSubsystem = SelectTargetSubsystem(combat, attacker, defender);
        var damage = CalculateEffectiveAttack(attacker, defender);
        if (damage <= 0)
        {
            return null;
        }

        var originalDamage = damage;
        if (defender.ShieldsCurrent > 0)
        {
            var shieldDamage = Math.Min(defender.ShieldsCurrent, damage);
            defender.ShieldsCurrent -= shieldDamage;
            defender.Subsystems[SubsystemType.Shields].CurrentHp = defender.ShieldsCurrent;
            damage -= shieldDamage;
        }

        var target = defender.Subsystems[targetSubsystem];
        if (damage > 0)
        {
            target.CurrentHp = Math.Max(0, target.CurrentHp - damage);
            target.IsOperational = target.CurrentHp > 0;

            if (targetSubsystem == SubsystemType.Hull)
            {
                defender.HullCurrent = target.CurrentHp;
            }
            else if (!target.IsOperational)
            {
                // Cascading failures: disabled critical systems cause hull instability.
                var cascadingDamage = targetSubsystem is SubsystemType.Reactor or SubsystemType.LifeSupport ? 8 : 4;
                defender.HullCurrent = Math.Max(0, defender.HullCurrent - cascadingDamage);
                defender.Subsystems[SubsystemType.Hull].CurrentHp = defender.HullCurrent;
            }
        }

        if (attacker == combat.Attacker)
        {
            combat.AttackerDamageDealt += originalDamage;
            combat.DefenderDamageTaken += originalDamage;
        }
        else
        {
            combat.DefenderDamageDealt += originalDamage;
            combat.AttackerDamageTaken += originalDamage;
        }

        return new SubsystemHitDto
        {
            AttackerShipId = attacker.ShipId,
            TargetShipId = defender.ShipId,
            TargetSubsystem = targetSubsystem,
            Damage = originalDamage,
            RemainingSubsystemHp = target.CurrentHp,
            SubsystemDisabled = !target.IsOperational
        };
    }

    private static int CalculateEffectiveAttack(CombatantState attacker, CombatantState defender)
    {
        var weaponTier = attacker.WeaponTier;
        var crewSkill = attacker.CrewCombatSkill;
        var energyAllocation = 0.6d + (attacker.Subsystems[SubsystemType.Reactor].HealthRatio * 0.4d);
        var doctrineBonus = attacker.DoctrineBonus;
        var positioningBonus = attacker.Subsystems[SubsystemType.Engines].HealthRatio * 10d;
        var targetShield = defender.ShieldsCurrent / 25d;
        var countermeasureReduction = defender.Subsystems[SubsystemType.Sensors].HealthRatio * 4d;

        // EffectiveAttack = (WeaponTier * CrewSkill * EnergyAllocation)
        // + DoctrineBonus + PositioningBonus - TargetShield - CountermeasureReduction
        var effectiveAttack =
            (weaponTier * crewSkill * energyAllocation) +
            doctrineBonus +
            positioningBonus -
            targetShield -
            countermeasureReduction;

        // Performance penalties from damaged systems.
        effectiveAttack *= attacker.Subsystems[SubsystemType.Weapons].HealthRatio;
        effectiveAttack *= attacker.Subsystems[SubsystemType.Reactor].HealthRatio;

        return (int)Math.Clamp(Math.Round(effectiveAttack), 0, 200);
    }

    private static SubsystemType SelectTargetSubsystem(
        CombatInternalState combat,
        CombatantState attacker,
        CombatantState defender)
    {
        if (defender.ShieldsCurrent > 0 && defender.Subsystems[SubsystemType.Shields].IsOperational)
        {
            return SubsystemType.Shields;
        }

        var seed = combat.TickCount + Math.Abs(attacker.ShipId.GetHashCode());
        var startIndex = seed % TargetOrder.Length;
        for (var offset = 0; offset < TargetOrder.Length; offset++)
        {
            var subsystem = TargetOrder[(startIndex + offset) % TargetOrder.Length];
            if (defender.Subsystems[subsystem].IsOperational || subsystem == SubsystemType.Hull)
            {
                return subsystem;
            }
        }

        return SubsystemType.Hull;
    }

    private static void RegenerateShields(CombatantState ship)
    {
        if (ship.HullCurrent <= 0)
        {
            return;
        }

        var shieldSubsystem = ship.Subsystems[SubsystemType.Shields];
        var reactorSubsystem = ship.Subsystems[SubsystemType.Reactor];
        if (!shieldSubsystem.IsOperational || !reactorSubsystem.IsOperational)
        {
            return;
        }

        var regen = (int)Math.Max(
            1,
            Math.Round(ship.MaxShields * 0.01d * shieldSubsystem.HealthRatio * reactorSubsystem.HealthRatio));
        ship.ShieldsCurrent = Math.Min(ship.MaxShields, ship.ShieldsCurrent + regen);
        shieldSubsystem.CurrentHp = ship.ShieldsCurrent;
    }

    private async Task PersistCombatLogAndEffectsAsync(
        CombatInternalState combat,
        CancellationToken cancellationToken)
    {
        var winnerShipId = combat.Attacker.HullCurrent == combat.Defender.HullCurrent
            ? (Guid?)null
            : combat.Attacker.HullCurrent > combat.Defender.HullCurrent
                ? combat.Attacker.ShipId
                : combat.Defender.ShipId;

        var attackerWon = winnerShipId == combat.Attacker.ShipId;
        var defenderWon = winnerShipId == combat.Defender.ShipId;

        var insurancePayout = 0m;
        if (defenderWon == false &&
            combat.Defender.Ship.HasInsurance &&
            combat.Defender.HullCurrent <= 0)
        {
            insurancePayout = Math.Round(combat.Defender.Ship.CurrentValue * combat.Defender.Ship.InsuranceRate, 2);
        }

        combat.Attacker.Ship.HullIntegrity = combat.Attacker.HullCurrent;
        combat.Defender.Ship.HullIntegrity = combat.Defender.HullCurrent;
        combat.Attacker.Ship.ShieldCapacity = combat.Attacker.ShieldsCurrent;
        combat.Defender.Ship.ShieldCapacity = combat.Defender.ShieldsCurrent;

        var locationSectorId = combat.Attacker.Ship.CurrentSectorId ??
            combat.Defender.Ship.CurrentSectorId ??
            Guid.Empty;

        var battleOutcome = attackerWon
            ? "victory"
            : defenderWon
                ? "defeat"
                : "draw";

        var now = combat.EndedAtUtc ?? DateTime.UtcNow;
        var log = new CombatLog
        {
            Id = Guid.NewGuid(),
            AttackerId = combat.Attacker.PlayerId,
            DefenderId = combat.Defender.PlayerId,
            LocationSectorId = locationSectorId,
            AttackerShipId = combat.Attacker.ShipId,
            DefenderShipId = combat.Defender.ShipId,
            AttackerInitialRating = combat.Attacker.InitialRating,
            DefenderInitialRating = combat.Defender.InitialRating,
            BattleOutcome = battleOutcome,
            AttackerDamageDealt = combat.AttackerDamageDealt,
            DefenderDamageDealt = combat.DefenderDamageDealt,
            AttackerHullDamage = combat.Attacker.MaxHull - combat.Attacker.HullCurrent,
            DefenderHullDamage = combat.Defender.MaxHull - combat.Defender.HullCurrent,
            AttackerReward = attackerWon ? 2_500m : 250m,
            DefenderCompensation = defenderWon ? 2_500m : 250m,
            InsurancePayout = insurancePayout,
            AttackerReputationChange = attackerWon ? 2 : -1,
            DefenderReputationChange = defenderWon ? 2 : -1,
            BattleStartedAt = combat.StartedAtUtc,
            BattleEndedAt = now,
            DurationSeconds = Math.Max(1, combat.TickCount / 4),
            TotalTicks = combat.TickCount
        };

        _dbContext.CombatLogs.Add(log);
        await UpdateCombatLeaderboardAsync(combat.Attacker.PlayerId, attackerWon ? 3 : 1, cancellationToken);
        await UpdateCombatLeaderboardAsync(combat.Defender.PlayerId, defenderWon ? 3 : 1, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task UpdateCombatLeaderboardAsync(
        Guid playerId,
        decimal scoreDelta,
        CancellationToken cancellationToken)
    {
        var entry = await _dbContext.Leaderboards
            .FirstOrDefaultAsync(
                leaderboard => leaderboard.PlayerId == playerId && leaderboard.LeaderboardType == "combat",
                cancellationToken);

        if (entry is null)
        {
            entry = new Leaderboard
            {
                Id = Guid.NewGuid(),
                PlayerId = playerId,
                LeaderboardType = "combat",
                Rank = 0,
                Score = scoreDelta,
                PreviousScore = 0m,
                LastUpdated = DateTime.UtcNow
            };
            _dbContext.Leaderboards.Add(entry);
        }
        else
        {
            entry.PreviousScore = entry.Score;
            entry.Score += scoreDelta;
            entry.LastUpdated = DateTime.UtcNow;
        }
    }

    private static CombatantState CreateCombatantState(Ship ship)
    {
        var maxHull = Math.Max(1, ship.HullIntegrity > 0 ? ship.HullIntegrity : ship.MaxHullIntegrity);
        var maxShields = Math.Max(0, ship.ShieldCapacity > 0 ? ship.ShieldCapacity : ship.MaxShieldCapacity);
        var crewSkill = ship.Crew.Count == 0 ? 40d : ship.Crew.Average(member => member.CombatSkill);
        var moduleTier = ship.Modules.Count == 0 ? 1d : ship.Modules.Average(module => Math.Max(1, module.Tier));
        var doctrineBonus = ship.Player?.ReputationScore / 20d ?? 0d;

        return new CombatantState
        {
            Ship = ship,
            ShipId = ship.Id,
            PlayerId = ship.PlayerId,
            InitialRating = (int)Math.Round((maxHull + maxShields) * 0.2 + (crewSkill * 0.4) + (moduleTier * 5)),
            MaxHull = maxHull,
            HullCurrent = maxHull,
            MaxShields = maxShields,
            ShieldsCurrent = maxShields,
            CrewCombatSkill = crewSkill,
            WeaponTier = moduleTier,
            DoctrineBonus = doctrineBonus,
            Subsystems = CreateSubsystems(maxHull, maxShields, ship)
        };
    }

    private static Dictionary<SubsystemType, SubsystemStatus> CreateSubsystems(int maxHull, int maxShields, Ship ship)
    {
        return new Dictionary<SubsystemType, SubsystemStatus>
        {
            [SubsystemType.Shields] = new() { Type = SubsystemType.Shields, MaxHp = Math.Max(1, maxShields), CurrentHp = Math.Max(0, maxShields), IsOperational = maxShields > 0 },
            [SubsystemType.Hull] = new() { Type = SubsystemType.Hull, MaxHp = maxHull, CurrentHp = maxHull, IsOperational = true },
            [SubsystemType.Engines] = new() { Type = SubsystemType.Engines, MaxHp = 100, CurrentHp = 100, IsOperational = true },
            [SubsystemType.Weapons] = new() { Type = SubsystemType.Weapons, MaxHp = 100, CurrentHp = 100, IsOperational = true },
            [SubsystemType.Sensors] = new() { Type = SubsystemType.Sensors, MaxHp = 100, CurrentHp = 100, IsOperational = true },
            [SubsystemType.Cargo] = new() { Type = SubsystemType.Cargo, MaxHp = 100, CurrentHp = 100, IsOperational = true },
            [SubsystemType.LifeSupport] = new() { Type = SubsystemType.LifeSupport, MaxHp = 100, CurrentHp = 100, IsOperational = true },
            [SubsystemType.Reactor] = new() { Type = SubsystemType.Reactor, MaxHp = Math.Max(1, ship.ReactorOutput), CurrentHp = Math.Max(1, ship.ReactorOutput), IsOperational = true }
        };
    }

    private static CombatTickResultDto MapTickResult(CombatInternalState combat, List<SubsystemHitDto> hits)
    {
        return new CombatTickResultDto
        {
            CombatId = combat.CombatId,
            TickNumber = combat.TickCount,
            State = combat.State,
            AttackerHull = combat.Attacker.HullCurrent,
            DefenderHull = combat.Defender.HullCurrent,
            AttackerShields = combat.Attacker.ShieldsCurrent,
            DefenderShields = combat.Defender.ShieldsCurrent,
            Hits = hits
        };
    }

    private static CombatSummaryDto MapSummary(CombatInternalState combat)
    {
        return new CombatSummaryDto
        {
            CombatId = combat.CombatId,
            State = combat.State,
            AttackerShipId = combat.Attacker.ShipId,
            DefenderShipId = combat.Defender.ShipId,
            WinnerShipId = combat.Attacker.HullCurrent == combat.Defender.HullCurrent
                ? (Guid?)null
                : combat.Attacker.HullCurrent > combat.Defender.HullCurrent
                    ? combat.Attacker.ShipId
                    : combat.Defender.ShipId,
            TickCount = combat.TickCount,
            MaxTicks = combat.MaxTicks,
            StartedAtUtc = combat.StartedAtUtc,
            EndedAtUtc = combat.EndedAtUtc,
            AttackerHull = combat.Attacker.HullCurrent,
            DefenderHull = combat.Defender.HullCurrent,
            AttackerSubsystems = combat.Attacker.Subsystems.Values.Select(MapSubsystem).ToList(),
            DefenderSubsystems = combat.Defender.Subsystems.Values.Select(MapSubsystem).ToList()
        };
    }

    private static SubsystemHealthDto MapSubsystem(SubsystemStatus subsystem)
    {
        return new SubsystemHealthDto
        {
            Type = subsystem.Type,
            CurrentHp = subsystem.CurrentHp,
            MaxHp = subsystem.MaxHp,
            IsOperational = subsystem.IsOperational
        };
    }

    private sealed class CombatInternalState
    {
        public required Guid CombatId { get; init; }
        public required CombatState State { get; set; }
        public required DateTime StartedAtUtc { get; init; }
        public DateTime? EndedAtUtc { get; set; }
        public required int MaxTicks { get; init; }
        public required int TickCount { get; set; }
        public required CombatantState Attacker { get; init; }
        public required CombatantState Defender { get; init; }
        public int AttackerDamageDealt { get; set; }
        public int DefenderDamageDealt { get; set; }
        public int AttackerDamageTaken { get; set; }
        public int DefenderDamageTaken { get; set; }
    }

    private sealed class CombatantState
    {
        public required Ship Ship { get; init; }
        public required Guid ShipId { get; init; }
        public required Guid PlayerId { get; init; }
        public required int InitialRating { get; init; }
        public required int MaxHull { get; init; }
        public required int HullCurrent { get; set; }
        public required int MaxShields { get; init; }
        public required int ShieldsCurrent { get; set; }
        public required double CrewCombatSkill { get; init; }
        public required double WeaponTier { get; init; }
        public required double DoctrineBonus { get; init; }
        public required Dictionary<SubsystemType, SubsystemStatus> Subsystems { get; init; }
    }

    private sealed class SubsystemStatus
    {
        public required SubsystemType Type { get; init; }
        public required int MaxHp { get; init; }
        public required int CurrentHp { get; set; }
        public required bool IsOperational { get; set; }
        public double HealthRatio => MaxHp <= 0 ? 0d : Math.Clamp((double)CurrentHp / MaxHp, 0d, 1d);
    }
}
