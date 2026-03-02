namespace GalacticTrader.MapGenerator.Api;

public sealed class CreateRouteApiRequest
{
    public required Guid FromSectorId { get; init; }
    public required Guid ToSectorId { get; init; }
    public required string LegalStatus { get; init; }
    public required string WarpGateType { get; init; }
}
