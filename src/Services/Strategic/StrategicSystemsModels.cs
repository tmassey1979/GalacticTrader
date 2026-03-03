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

public sealed class UpsertInsurancePolicyRequest
{
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public float CoverageRate { get; init; }
    public decimal PremiumPerCycle { get; init; }
    public string RiskTier { get; init; } = string.Empty;
    public bool IsActive { get; init; } = true;
}

public sealed class InsurancePolicyDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public string ShipName { get; init; } = string.Empty;
    public float CoverageRate { get; init; }
    public decimal PremiumPerCycle { get; init; }
    public string RiskTier { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime? LastPremiumChargedAt { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class FileInsuranceClaimRequest
{
    public Guid PolicyId { get; init; }
    public Guid? CombatLogId { get; init; }
    public decimal ClaimAmount { get; init; }
}

public sealed class InsuranceClaimDto
{
    public Guid Id { get; init; }
    public Guid PolicyId { get; init; }
    public Guid PlayerId { get; init; }
    public Guid ShipId { get; init; }
    public decimal ClaimAmount { get; init; }
    public float FraudRiskScore { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime FiledAt { get; init; }
    public DateTime? ResolvedAt { get; init; }
}

public sealed class CreateIntelligenceNetworkRequest
{
    public Guid OwnerPlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AssetCount { get; init; }
    public float CoverageScore { get; init; }
}

public sealed class IntelligenceNetworkDto
{
    public Guid Id { get; init; }
    public Guid OwnerPlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int AssetCount { get; init; }
    public float CoverageScore { get; init; }
    public bool IsActive { get; init; }
    public DateTime UpdatedAt { get; init; }
}

public sealed class PublishIntelligenceReportRequest
{
    public Guid NetworkId { get; init; }
    public Guid SectorId { get; init; }
    public string SignalType { get; init; } = string.Empty;
    public float ConfidenceScore { get; init; }
    public string Payload { get; init; } = string.Empty;
    public int TtlMinutes { get; init; } = 30;
}

public sealed class IntelligenceReportDto
{
    public Guid Id { get; init; }
    public Guid NetworkId { get; init; }
    public string NetworkName { get; init; } = string.Empty;
    public Guid SectorId { get; init; }
    public string SectorName { get; init; } = string.Empty;
    public string SignalType { get; init; } = string.Empty;
    public float ConfidenceScore { get; init; }
    public string Payload { get; init; } = string.Empty;
    public DateTime DetectedAt { get; init; }
    public DateTime ExpiresAt { get; init; }
    public bool IsExpired { get; init; }
}
