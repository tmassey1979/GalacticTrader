namespace GalacticTrader.Desktop.Api;

public sealed class ConvoySimulationApiRequest
{
    public Guid PlayerId { get; init; }
    public int Formation { get; init; }
    public decimal ConvoyValue { get; init; }
}
