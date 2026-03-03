namespace GalacticTrader.Services.Fleet;

public sealed class ShipTemplateDto
{
    public string Key { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int ShieldCapacity { get; init; }
    public int ReactorOutput { get; init; }
    public int CargoCapacity { get; init; }
    public int SensorRange { get; init; }
    public int SignatureProfile { get; init; }
    public int CrewSlots { get; init; }
    public int Hardpoints { get; init; }
    public decimal PurchasePrice { get; init; }
}
