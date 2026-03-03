namespace GalacticTrader.Desktop.Trading;

public sealed class TradeSupplyDemandCurveSnapshot
{
    public long DemandUnits { get; init; }
    public long SupplyUnits { get; init; }
    public decimal DemandRatio { get; init; }
    public decimal SupplyRatio { get; init; }
    public IReadOnlyList<TradeSupplyDemandCurvePoint> Points { get; init; } = [];
}
