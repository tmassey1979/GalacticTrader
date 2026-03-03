namespace GalacticTrader.Desktop.Modules;

public sealed class ReputationStandingMatrixDisplayRow
{
    public required string FactionId { get; init; }
    public int ReputationScore { get; init; }
    public double NormalizedScorePercent { get; init; }
    public required string ScoreSummary { get; init; }
}
