namespace GalacticTrader.Services.Navigation;

public sealed class GraphValidationReport
{
    public int SectorCount { get; init; }
    public int RouteCount { get; init; }
    public List<string> Errors { get; } = [];
    public List<string> Warnings { get; } = [];
    public bool IsValid => Errors.Count == 0;
}
