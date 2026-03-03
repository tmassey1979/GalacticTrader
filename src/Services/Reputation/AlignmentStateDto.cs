namespace GalacticTrader.Services.Reputation;

public sealed class AlignmentStateDto
{
    public Guid PlayerId { get; init; }
    public int AlignmentLevel { get; init; }
    public string Path { get; init; } = string.Empty;
    public float ScanFrequencyModifier { get; init; }
    public float InsuranceCostModifier { get; init; }
    public IReadOnlyList<string> AccessRestrictions { get; init; } = [];
}
