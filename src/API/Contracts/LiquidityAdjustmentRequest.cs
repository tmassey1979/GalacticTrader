namespace GalacticTrader.API.Contracts;

public sealed record LiquidityAdjustmentRequest(decimal DeltaPercent, string? Reason);
