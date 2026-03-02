namespace GalacticTrader.Services.Strategic;

public sealed class UpdateSectorVolatilityCycleRequest
{
    public Guid SectorId { get; init; }
    public string CurrentPhase { get; init; } = "stable";
    public float VolatilityIndex { get; init; }
    public DateTime? NextTransitionAt { get; init; }
}

public sealed class SectorVolatilityCycleDto
{
    public Guid Id { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public string CurrentPhase { get; init; } = string.Empty;
    public float VolatilityIndex { get; init; }
    public DateTime CycleStartedAt { get; init; }
    public DateTime NextTransitionAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}

public sealed class DeclareCorporateWarRequest
{
    public Guid AttackerFactionId { get; init; }
    public Guid DefenderFactionId { get; init; }
    public string CasusBelli { get; init; } = string.Empty;
    public int Intensity { get; init; }
}

public sealed class CorporateWarDto
{
    public Guid Id { get; init; }
    public Guid AttackerFactionId { get; init; }
    public string AttackerFactionName { get; init; } = string.Empty;
    public Guid DefenderFactionId { get; init; }
    public string DefenderFactionName { get; init; } = string.Empty;
    public string CasusBelli { get; init; } = string.Empty;
    public int Intensity { get; init; }
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public bool IsActive { get; init; }
}

public sealed class UpdateInfrastructureOwnershipRequest
{
    public Guid SectorId { get; init; }
    public Guid FactionId { get; init; }
    public string InfrastructureType { get; init; } = string.Empty;
    public float ControlScore { get; init; }
}

public sealed class InfrastructureOwnershipDto
{
    public Guid Id { get; init; }
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public string InfrastructureType { get; init; } = string.Empty;
    public float ControlScore { get; init; }
    public DateTime ClaimedAt { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}

public sealed class TerritoryDominanceDto
{
    public Guid Id { get; init; }
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public int ControlledSectorCount { get; init; }
    public float InfrastructureControlScore { get; init; }
    public float WarMomentumScore { get; init; }
    public float DominanceScore { get; init; }
    public DateTime UpdatedAt { get; init; }
}
