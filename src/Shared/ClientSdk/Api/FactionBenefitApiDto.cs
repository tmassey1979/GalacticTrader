namespace GalacticTrader.Desktop.Api;

public sealed class FactionBenefitApiDto
{
    public Guid FactionId { get; init; }
    public string FactionName { get; init; } = string.Empty;
    public string Tier { get; init; } = string.Empty;
    public IReadOnlyList<string> Benefits { get; init; } = [];
}
