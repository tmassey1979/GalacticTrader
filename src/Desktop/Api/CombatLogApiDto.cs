namespace GalacticTrader.Desktop.Api;

public sealed class CombatLogApiDto
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
