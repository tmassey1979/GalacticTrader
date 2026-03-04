using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Unity.Auth;

public static class AuthFailureMessageMapper
{
    public static string ToUserMessage(AuthOperationResult result)
    {
        if (result.Succeeded)
        {
            return string.Empty;
        }

        return result.FailureState switch
        {
            AuthFailureState.SessionMissing => "Session not found. Please sign in.",
            AuthFailureState.InvalidCredentials => "Invalid username or password.",
            AuthFailureState.Unauthorized => "Session is no longer authorized. Please sign in again.",
            AuthFailureState.Forbidden => "Your account is not allowed to perform this action.",
            AuthFailureState.TokenExpired => "Session expired. Please sign in again.",
            AuthFailureState.RefreshFailed => "Session refresh failed. Please sign in again.",
            AuthFailureState.NetworkError => "Network error while contacting authentication services.",
            AuthFailureState.BackendError => "Authentication service is unavailable. Try again shortly.",
            _ => string.IsNullOrWhiteSpace(result.Message)
                ? "Authentication failed. Please try again."
                : result.Message
        };
    }
}
