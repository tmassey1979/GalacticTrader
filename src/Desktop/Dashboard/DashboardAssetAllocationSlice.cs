namespace GalacticTrader.Desktop.Dashboard;

public sealed class DashboardAssetAllocationSlice
{
    public required string Label { get; init; }
    public decimal Value { get; init; }
    public decimal Percent { get; init; }
    public required string PieGlyph { get; init; }
}
