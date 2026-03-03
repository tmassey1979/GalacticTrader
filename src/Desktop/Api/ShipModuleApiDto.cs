namespace GalacticTrader.Desktop.Api;

public sealed class ShipModuleApiDto
{
    public Guid Id { get; init; }
    public string ModuleType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Tier { get; init; }
    public decimal PurchasePrice { get; init; }
}
