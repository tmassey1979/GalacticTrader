namespace GalacticTrader.Services.Admin;

public sealed class BalanceControlSnapshot
{
    public required decimal TaxRatePercent { get; init; }
    public required int PirateIntensityPercent { get; init; }
    public required decimal LiquidityAdjustmentPercent { get; init; }
    public required decimal EconomicCorrectionPercent { get; init; }
    public required DateTime LastUpdatedUtc { get; init; }
    public required string LastAction { get; init; }
    public required IReadOnlyList<Guid> UnstableSectors { get; init; }
}
