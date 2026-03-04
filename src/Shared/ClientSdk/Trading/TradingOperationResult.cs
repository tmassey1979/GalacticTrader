using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Trading;

public sealed class TradingOperationResult
{
    private TradingOperationResult(
        bool succeeded,
        TradingOperationFailureState failureState,
        string message,
        TradeExecutionResultApiDto? transaction,
        string? errorDetail)
    {
        Succeeded = succeeded;
        FailureState = failureState;
        Message = message;
        Transaction = transaction;
        ErrorDetail = errorDetail;
    }

    public bool Succeeded { get; }

    public TradingOperationFailureState FailureState { get; }

    public string Message { get; }

    public string? ErrorDetail { get; }

    public TradeExecutionResultApiDto? Transaction { get; }

    public static TradingOperationResult Success(TradeExecutionResultApiDto transaction, string message)
        => new(
            succeeded: true,
            failureState: TradingOperationFailureState.None,
            message: message,
            transaction: transaction,
            errorDetail: null);

    public static TradingOperationResult Failure(
        TradingOperationFailureState failureState,
        string message,
        string? errorDetail = null)
        => new(
            succeeded: false,
            failureState: failureState,
            message: message,
            transaction: null,
            errorDetail: errorDetail);
}
