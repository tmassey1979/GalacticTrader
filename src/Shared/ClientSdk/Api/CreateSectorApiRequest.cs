namespace GalacticTrader.Desktop.Api;

public sealed class CreateSectorApiRequest
{
    public required string Name { get; init; }
    public required float X { get; init; }
    public required float Y { get; init; }
    public required float Z { get; init; }
}
