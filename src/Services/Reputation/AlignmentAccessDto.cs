namespace GalacticTrader.Services.Reputation;

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
