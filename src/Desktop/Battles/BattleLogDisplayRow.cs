namespace GalacticTrader.Desktop.Battles;

public sealed class BattleLogDisplayRow
{
    public DateTime EndedAtUtc { get; init; }
    public required string Outcome { get; init; }
    public int AttackerRating { get; init; }
    public int DefenderRating { get; init; }
    public int ReputationDelta { get; init; }
    public decimal ResourceChange { get; init; }
    public decimal EnvironmentalModifier { get; init; }
    public decimal ProtectionModifier { get; init; }
    public decimal EconomicImpactProjection { get; init; }
    public required string DamageReport { get; init; }
    public int DurationSeconds { get; init; }
    public int TotalTicks { get; init; }
    public required string Attacker { get; init; }
    public required string Defender { get; init; }
    public decimal InsurancePayout { get; init; }
}
