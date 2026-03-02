namespace GalacticTrader.Services.Reputation;

public enum AlignmentActionType
{
    LegalTrade,
    InfrastructureInvestment,
    InsuranceClaim,
    Piracy,
    Smuggling,
    Sabotage,
    SensorSpoofing
}

public sealed class UpdateFactionStandingRequest
{
    public Guid PlayerId { get; init; }
    public Guid FactionId { get; init; }
    public int ReputationDelta { get; init; }
    public string Reason { get; init; } = string.Empty;
}

public sealed class PlayerFactionStandingDto
{
    public Guid PlayerId { get; init; }
    public Guid FactionId { get; init; }
    public int ReputationScore { get; init; }
    public string Tier { get; init; } = string.Empty;
    public bool HasAccess { get; init; }
    public decimal TradingDiscount { get; init; }
    public decimal TaxModifier { get; init; }
    public IReadOnlyList<string> Benefits { get; init; } = [];
}

public sealed class AlignmentActionRequest
{
    public Guid PlayerId { get; init; }
    public AlignmentActionType ActionType { get; init; }
    public int Magnitude { get; init; } = 1;
}

public sealed class AlignmentStateDto
{
    public Guid PlayerId { get; init; }
    public int AlignmentLevel { get; init; }
    public string Path { get; init; } = string.Empty;
    public float ScanFrequencyModifier { get; init; }
    public float InsuranceCostModifier { get; init; }
    public IReadOnlyList<string> AccessRestrictions { get; init; } = [];
}

public sealed class AlignmentAccessDto
{
    public Guid PlayerId { get; init; }
    public int AlignmentLevel { get; init; }
    public string Path { get; init; } = string.Empty;
    public bool CanUseLegalInsurance { get; init; }
    public bool CanAccessBlackMarket { get; init; }
    public float ScanFrequencyModifier { get; init; }
    public float InsuranceCostModifier { get; init; }
}

public sealed class FactionBenefitDto
{
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public IReadOnlyList<string> Benefits { get; init; } = [];
}
