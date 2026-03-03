namespace GalacticTrader.Services.Navigation;

internal sealed class TravelModeProfile
{
    public required TravelMode Mode { get; init; }
    public required double TimeMultiplier { get; init; }
    public required double FuelMultiplier { get; init; }
    public required double RiskMultiplier { get; init; }
}
