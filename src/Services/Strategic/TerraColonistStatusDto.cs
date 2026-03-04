namespace GalacticTrader.Services.Strategic;

public sealed class TerraColonistStatusDto
{
    public Guid SourceId { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public long AvailableColonists { get; init; }
    public int OutputPerMinute { get; init; }
    public long StorageCapacity { get; init; }
    public DateTime LastGeneratedAtUtc { get; init; }
    public DateTime UpdatedAtUtc { get; init; }
}
