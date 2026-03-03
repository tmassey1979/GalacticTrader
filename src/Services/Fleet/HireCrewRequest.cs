namespace GalacticTrader.Services.Fleet;

public sealed class HireCrewRequest
{
    public Guid PlayerId { get; init; }
    public Guid? ShipId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public decimal Salary { get; init; }
}
