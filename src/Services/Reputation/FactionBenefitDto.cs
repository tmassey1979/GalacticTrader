namespace GalacticTrader.Services.Reputation;

public sealed class FactionBenefitDto
{
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public IReadOnlyList<string> Benefits { get; init; } = [];
}
