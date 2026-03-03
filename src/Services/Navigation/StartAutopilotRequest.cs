namespace GalacticTrader.Services.Navigation;

public sealed class StartAutopilotRequest
{
    public Guid ShipId { get; init; }
    public Guid FromSectorId { get; init; }
    public Guid ToSectorId { get; init; }
    public TravelMode TravelMode { get; init; } = TravelMode.Standard;
    public decimal CargoValue { get; init; }
    public int PlayerNotoriety { get; init; }
    public int EscortStrength { get; init; }
    public int FactionProtection { get; init; }
}
