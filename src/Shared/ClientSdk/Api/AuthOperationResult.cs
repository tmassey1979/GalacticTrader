namespace GalacticTrader.Desktop.Api;

public sealed record AuthOperationResult
{
    public bool Succeeded { get; init; }

    public DesktopSession? Session { get; init; }

    public AuthFailureState FailureState { get; init; }

    public string Message { get; init; } = string.Empty;

    public Exception? Exception { get; init; }

    public static AuthOperationResult Success(DesktopSession? session, string message)
    {
        return new AuthOperationResult
        {
            Succeeded = true,
            Session = session,
            FailureState = AuthFailureState.None,
            Message = message
        };
    }

    public static AuthOperationResult Failure(AuthFailureState failureState, string message, Exception? exception = null)
    {
        return new AuthOperationResult
        {
            Succeeded = false,
            Session = null,
            FailureState = failureState,
            Message = message,
            Exception = exception
        };
    }
}
