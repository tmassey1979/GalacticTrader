namespace GalacticTrader.Services.Navigation;

public sealed class SectorDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
    public int SecurityLevel { get; init; }
    public int HazardRating { get; init; }
    public float ResourceModifier { get; init; }
    public int EconomicIndex { get; init; }
    public float SensorInterferenceLevel { get; init; }
    public Guid? ControlledByFactionId { get; init; }
    public string? ControlledByFactionName { get; init; }
    public int ConnectedSectorCount { get; init; }
}
