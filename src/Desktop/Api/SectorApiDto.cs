namespace GalacticTrader.Desktop.Api;

public sealed class SectorApiDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public float X { get; init; }
    public float Y { get; init; }
    public float Z { get; init; }
}
