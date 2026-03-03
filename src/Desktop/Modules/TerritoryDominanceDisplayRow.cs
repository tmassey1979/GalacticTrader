namespace GalacticTrader.Desktop.Modules;

public sealed class TerritoryDominanceDisplayRow
{
    public Guid FactionId { get; init; }
    public required string FactionName { get; init; }
    public int ControlledSectorCount { get; init; }
    public float InfrastructureControlScore { get; init; }
    public float WarMomentumScore { get; init; }
    public float DominanceScore { get; init; }
    public required string HeatHex { get; init; }
    public required string ProtectionPriority { get; init; }
}
