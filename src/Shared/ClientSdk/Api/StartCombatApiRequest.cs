namespace GalacticTrader.Desktop.Api;

public sealed class StartCombatApiRequest
{
    public Guid AttackerShipId { get; init; }
    public Guid DefenderShipId { get; init; }
    public int MaxTicks { get; init; } = 600;
}
