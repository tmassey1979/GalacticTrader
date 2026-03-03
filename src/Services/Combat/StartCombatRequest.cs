namespace GalacticTrader.Services.Combat;

public sealed class StartCombatRequest
{
    public Guid AttackerShipId { get; init; }
    public Guid DefenderShipId { get; init; }
    public int MaxTicks { get; init; } = 600;
}
