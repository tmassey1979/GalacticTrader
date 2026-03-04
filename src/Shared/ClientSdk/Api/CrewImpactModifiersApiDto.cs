namespace GalacticTrader.Desktop.Api;

public sealed class CrewImpactModifiersApiDto
{
    public float CombatModifier { get; init; }
    public float EngineeringModifier { get; init; }
    public float NavigationModifier { get; init; }
    public float MoraleFactor { get; init; }
    public float LoyaltyFactor { get; init; }
}
