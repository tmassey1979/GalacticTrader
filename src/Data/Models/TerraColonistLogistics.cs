using System;

namespace GalacticTrader.Data.Models;

public sealed class TerraColonistSource
{
    public Guid Id { get; set; }
    public Guid SectorId { get; set; }
    public long AvailableColonists { get; set; }
    public int OutputPerMinute { get; set; }
    public long StorageCapacity { get; set; }
    public DateTime LastGeneratedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public Sector Sector { get; set; } = null!;
}

public sealed class ColonistShipment
{
    public Guid Id { get; set; }
    public Guid PlayerId { get; set; }
    public Guid FromSectorId { get; set; }
    public Guid DestinationSectorId { get; set; }
    public long ColonistCount { get; set; }
    public int RouteTravelSeconds { get; set; }
    public float EstimatedRiskScore { get; set; }
    public string TravelMode { get; set; } = string.Empty;
    public DateTime LoadedAtUtc { get; set; }
    public DateTime EstimatedArrivalAtUtc { get; set; }
    public DateTime? DeliveredAtUtc { get; set; }
    public string Status { get; set; } = string.Empty;

    public Player Player { get; set; } = null!;
    public Sector FromSector { get; set; } = null!;
    public Sector DestinationSector { get; set; } = null!;
}

public sealed class ColonistDeliveryAudit
{
    public Guid Id { get; set; }
    public Guid ShipmentId { get; set; }
    public Guid PlayerId { get; set; }
    public Guid DestinationSectorId { get; set; }
    public long ColonistCount { get; set; }
    public float EstimatedRiskScore { get; set; }
    public DateTime DeliveredAtUtc { get; set; }

    public ColonistShipment Shipment { get; set; } = null!;
    public Player Player { get; set; } = null!;
    public Sector DestinationSector { get; set; } = null!;
}
