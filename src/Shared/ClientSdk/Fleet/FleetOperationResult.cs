using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Fleet;

public sealed class FleetOperationResult
{
    private FleetOperationResult(
        bool succeeded,
        FleetOperationFailureState failureState,
        string message,
        ShipApiDto? ship,
        string? errorDetail)
    {
        Succeeded = succeeded;
        FailureState = failureState;
        Message = message;
        Ship = ship;
        ErrorDetail = errorDetail;
    }

    public bool Succeeded { get; }

    public FleetOperationFailureState FailureState { get; }

    public string Message { get; }

    public ShipApiDto? Ship { get; }

    public string? ErrorDetail { get; }

    public static FleetOperationResult Success(ShipApiDto ship, string message)
        => new(
            succeeded: true,
            failureState: FleetOperationFailureState.None,
            message: message,
            ship: ship,
            errorDetail: null);

    public static FleetOperationResult Failure(
        FleetOperationFailureState failureState,
        string message,
        string? errorDetail = null)
        => new(
            succeeded: false,
            failureState: failureState,
            message: message,
            ship: null,
            errorDetail: errorDetail);
}
