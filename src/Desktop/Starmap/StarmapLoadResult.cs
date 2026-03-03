namespace GalacticTrader.Desktop.Starmap;

public sealed class StarmapLoadResult
{
    public required StarmapScene Scene { get; init; }
    public bool UsedFallback { get; init; }
    public string Warning { get; init; } = string.Empty;
}
