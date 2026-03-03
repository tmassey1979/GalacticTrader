namespace GalacticTrader.Services.Navigation;

public sealed class TransitionTravelModeRequest
{
    public TravelMode TargetMode { get; init; }
    public string Reason { get; init; } = string.Empty;
}
