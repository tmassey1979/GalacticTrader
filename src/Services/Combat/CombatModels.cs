namespace GalacticTrader.Services.Combat;

public enum CombatState
{
    Initializing,
    Active,
    Completed,
    Cancelled
}

public enum SubsystemType
{
    Shields,
    Hull,
    Engines,
    Weapons,
    Sensors,
    Cargo,
    LifeSupport,
    Reactor
}

public sealed class StartCombatRequest
{
    public Guid AttackerShipId { get; init; }
    public Guid DefenderShipId { get; init; }
    public int MaxTicks { get; init; } = 600; // 150 seconds at 250ms
}

public sealed class CombatTickResultDto
{
    public Guid CombatId { get; init; }
    public int TickNumber { get; init; }
    public CombatState State { get; init; }
    public int AttackerHull { get; init; }
    public int DefenderHull { get; init; }
    public int AttackerShields { get; init; }
    public int DefenderShields { get; init; }
    public List<SubsystemHitDto> Hits { get; init; } = [];
}

public sealed class SubsystemHitDto
{
    public Guid AttackerShipId { get; init; }
    public Guid TargetShipId { get; init; }
    public SubsystemType TargetSubsystem { get; init; }
    public int Damage { get; init; }
    public int RemainingSubsystemHp { get; init; }
    public bool SubsystemDisabled { get; init; }
}

public sealed class CombatSummaryDto
{
    public Guid CombatId { get; init; }
    public CombatState State { get; init; }
    public Guid AttackerShipId { get; init; }
    public Guid DefenderShipId { get; init; }
    public Guid? WinnerShipId { get; init; }
    public int TickCount { get; init; }
    public int MaxTicks { get; init; }
    public DateTime StartedAtUtc { get; init; }
    public DateTime? EndedAtUtc { get; init; }
    public int AttackerHull { get; init; }
    public int DefenderHull { get; init; }
    public IReadOnlyList<SubsystemHealthDto> AttackerSubsystems { get; init; } = [];
    public IReadOnlyList<SubsystemHealthDto> DefenderSubsystems { get; init; } = [];
}

public sealed class SubsystemHealthDto
{
    public SubsystemType Type { get; init; }
    public int CurrentHp { get; init; }
    public int MaxHp { get; init; }
    public bool IsOperational { get; init; }
}

public sealed class CombatLogDto
{
    public Guid Id { get; init; }
    public Guid AttackerId { get; init; }
    public Guid? DefenderId { get; init; }
    public Guid AttackerShipId { get; init; }
    public Guid? DefenderShipId { get; init; }
    public string BattleOutcome { get; init; } = string.Empty;
    public DateTime BattleStartedAt { get; init; }
    public DateTime BattleEndedAt { get; init; }
    public int DurationSeconds { get; init; }
    public int TotalTicks { get; init; }
    public decimal InsurancePayout { get; init; }
}
