namespace GalacticTrader.Desktop.Modules;

public sealed class CommunicationMessageDisplayRow
{
    public DateTime CreatedAtUtc { get; init; }
    public string Sender { get; init; } = string.Empty;
    public string Channel { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string Moderation { get; init; } = string.Empty;
}
