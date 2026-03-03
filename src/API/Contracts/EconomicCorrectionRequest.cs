namespace GalacticTrader.API.Contracts;

public sealed record EconomicCorrectionRequest(decimal AdjustmentPercent, string? Reason);
