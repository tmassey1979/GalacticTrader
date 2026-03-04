namespace GalacticTrader.API.Contracts;

public sealed class UpdateTerraColonistSourceApiRequest
{
    public Guid? SectorId { get; init; }
    public int OutputPerMinute { get; init; }
    public long StorageCapacity { get; init; }
    public long AvailableColonists { get; init; }
}
