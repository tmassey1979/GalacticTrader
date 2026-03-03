namespace GalacticTrader.Services.Fleet;

public sealed class CrewImpactModifiersDto
{
    public float CombatModifier { get; init; }
    public float EngineeringModifier { get; init; }
    public float NavigationModifier { get; init; }
    public float MoraleFactor { get; init; }
    public float LoyaltyFactor { get; init; }
}
