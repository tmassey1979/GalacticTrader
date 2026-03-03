namespace GalacticTrader.Services.Npc;

public sealed class NpcShipDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int CombatRating { get; init; }
    public Guid? CurrentSectorId { get; init; }
    public bool IsActive { get; init; }
}
