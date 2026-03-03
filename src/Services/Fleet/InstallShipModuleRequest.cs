namespace GalacticTrader.Services.Fleet;

public sealed class InstallShipModuleRequest
{
    public Guid ShipId { get; init; }
    public string ModuleType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Tier { get; init; } = 1;
}
