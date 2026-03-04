namespace GalacticTrader.Desktop.Api;

public sealed class PurchaseShipApiRequest
{
    public Guid PlayerId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}
