using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Trading;

public sealed record TradingPreviewResult(
    PricePreviewApiResultDto Preview,
    TradingPreviewSummary Summary);
