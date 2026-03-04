namespace GalacticTrader.Desktop.Api;

public sealed class EscortSummaryApiDto
{
    public Guid PlayerId { get; init; }
    public int FleetStrength { get; init; }
    public int EscortStrength { get; init; }
    public float ConvoyBonus { get; init; }
    public int Formation { get; init; }
    public float ProtectiveRange { get; init; }
    public float CoordinationBonus { get; init; }
    public float CombatModifier { get; init; }
    public CrewImpactModifiersApiDto CrewImpact { get; init; } = new();
}
