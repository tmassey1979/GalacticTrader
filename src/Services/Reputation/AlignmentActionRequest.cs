namespace GalacticTrader.Services.Reputation;

public sealed class AlignmentActionRequest
{
    public Guid PlayerId { get; init; }
    public AlignmentActionType ActionType { get; init; }
    public int Magnitude { get; init; } = 1;
}
