using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Trading;

public sealed record TradingModuleState(
    IReadOnlyList<MarketListingApiDto> Listings,
    IReadOnlyList<TradeExecutionResultApiDto> Transactions,
    IReadOnlyList<TradingListingSummary> ListingSummaries,
    decimal AvailableCredits,
    DateTime LoadedAtUtc);
