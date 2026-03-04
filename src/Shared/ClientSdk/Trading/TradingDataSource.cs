using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Trading;

public sealed class TradingDataSource
{
    public required Func<int, CancellationToken, Task<IReadOnlyList<MarketListingApiDto>>> LoadListingsAsync { get; init; }

    public required Func<Guid, int, CancellationToken, Task<IReadOnlyList<TradeExecutionResultApiDto>>> LoadTransactionsAsync { get; init; }

    public required Func<PricePreviewApiRequest, CancellationToken, Task<PricePreviewApiResultDto>> PreviewPriceAsync { get; init; }

    public required Func<ExecuteTradeApiRequest, CancellationToken, Task<TradeExecutionResultApiDto>> ExecuteTradeAsync { get; init; }
}
