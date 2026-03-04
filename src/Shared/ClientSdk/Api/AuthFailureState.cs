namespace GalacticTrader.Desktop.Api;

public enum AuthFailureState
{
    None = 0,
    SessionMissing = 1,
    InvalidCredentials = 2,
    Unauthorized = 3,
    Forbidden = 4,
    TokenExpired = 5,
    RefreshFailed = 6,
    NetworkError = 7,
    BackendError = 8,
    Unknown = 9
}
