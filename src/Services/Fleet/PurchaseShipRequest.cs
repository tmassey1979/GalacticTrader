namespace GalacticTrader.Services.Fleet;

public sealed class PurchaseShipRequest
{
    public Guid PlayerId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
