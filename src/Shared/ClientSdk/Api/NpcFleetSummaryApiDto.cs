namespace GalacticTrader.Desktop.Api;

public sealed class NpcFleetSummaryApiDto
{
    public Guid AgentId { get; init; }
    public int FleetSize { get; init; }
    public int ActiveShips { get; init; }
    public float CoordinationBonus { get; init; }
}
