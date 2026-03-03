namespace GalacticTrader.Desktop.Api;

public sealed class ShipApiDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int MaxHullIntegrity { get; init; }
    public int ShieldCapacity { get; init; }
    public int MaxShieldCapacity { get; init; }
    public int ReactorOutput { get; init; }
    public int CargoCapacity { get; init; }
    public int SensorRange { get; init; }
    public int SignatureProfile { get; init; }
    public int CrewSlots { get; init; }
    public int Hardpoints { get; init; }
    public bool HasInsurance { get; init; }
    public decimal InsuranceRate { get; init; }
    public string AssignedRoute { get; init; } = string.Empty;
    public decimal CurrentValue { get; init; }
    public IReadOnlyList<ShipModuleApiDto> Modules { get; init; } = [];
    public int CrewCount { get; init; }
}
