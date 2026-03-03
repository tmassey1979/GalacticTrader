namespace GalacticTrader.Desktop.Trading;

public sealed class NpcCompetitorDisplayRow
{
    public string Name { get; init; } = string.Empty;
    public string Archetype { get; init; } = string.Empty;
    public int FleetSize { get; init; }
    public float InfluenceScore { get; init; }
    public float PresenceScore { get; init; }
}
