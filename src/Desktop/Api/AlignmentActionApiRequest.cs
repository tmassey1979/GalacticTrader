namespace GalacticTrader.Desktop.Api;

public sealed class AlignmentActionApiRequest
{
    public Guid PlayerId { get; init; }
    public string ActionType { get; init; } = string.Empty;
    public int Magnitude { get; init; } = 1;
}
