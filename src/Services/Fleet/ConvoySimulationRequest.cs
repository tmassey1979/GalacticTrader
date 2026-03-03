namespace GalacticTrader.Services.Fleet;

public sealed class ConvoySimulationRequest
{
    public Guid PlayerId { get; init; }
    public FleetFormation Formation { get; init; } = FleetFormation.Defensive;
    public decimal ConvoyValue { get; init; }
}
