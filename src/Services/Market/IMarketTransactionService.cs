namespace GalacticTrader.Services.Market;

public interface IMarketTransactionService
{
    Task<TradeExecutionResult> ExecuteTradeAsync(
        ExecuteTradeRequest request,
        CancellationToken cancellationToken = default);

    Task<TradeExecutionResult?> ReverseTradeAsync(
        ReverseTradeRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TradeExecutionResult>> GetPlayerTransactionsAsync(
        Guid playerId,
        int limit = 50,
        CancellationToken cancellationToken = default);
}
