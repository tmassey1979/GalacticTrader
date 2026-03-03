namespace GalacticTrader.Services.Npc;

public sealed class NpcFleetSummary
{
    public Guid AgentId { get; init; }
    public int FleetSize { get; init; }
    public int ActiveShips { get; init; }
    public float CoordinationBonus { get; init; }
    public IReadOnlyList<NpcShipDto> Ships { get; init; } = [];
}
