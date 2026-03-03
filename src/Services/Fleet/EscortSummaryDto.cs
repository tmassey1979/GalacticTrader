namespace GalacticTrader.Services.Fleet;

public sealed class EscortSummaryDto
{
    public Guid PlayerId { get; init; }
    public int FleetStrength { get; init; }
    public int EscortStrength { get; init; }
    public float ConvoyBonus { get; init; }
    public FleetFormation Formation { get; init; }
    public float ProtectiveRange { get; init; }
    public float CoordinationBonus { get; init; }
    public float CombatModifier { get; init; }
    public CrewImpactModifiersDto CrewImpact { get; init; } = new();
}
