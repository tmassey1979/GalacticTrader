using System.Net;
using System.Text.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class AuthSessionManager
{
    private readonly IAuthApiClient _authApiClient;
    private readonly IClientSessionStore _sessionStore;
    private readonly Func<DesktopSession, CancellationToken, Task<DesktopSession?>>? _refreshSessionAsync;
    private readonly TimeSpan _expiryGracePeriod;

    public AuthSessionManager(
        IAuthApiClient authApiClient,
        IClientSessionStore sessionStore,
        Func<DesktopSession, CancellationToken, Task<DesktopSession?>>? refreshSessionAsync = null,
        TimeSpan? expiryGracePeriod = null)
    {
        _authApiClient = authApiClient;
        _sessionStore = sessionStore;
        _refreshSessionAsync = refreshSessionAsync;
        _expiryGracePeriod = expiryGracePeriod ?? TimeSpan.FromSeconds(30);
    }

    public DesktopSession? CurrentSession { get; private set; }

    public async Task<AuthOperationResult> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var session = await _authApiClient.LoginAsync(username, password, cancellationToken);
            await _sessionStore.SaveAsync(session, cancellationToken);
            CurrentSession = session;
            return AuthOperationResult.Success(session, "Authenticated successfully.");
        }
        catch (Exception exception)
        {
            return MapFailureResult(exception, fallbackMessage: "Authentication failed.");
        }
    }

    public async Task<AuthOperationResult> RestoreSessionAsync(CancellationToken cancellationToken = default)
    {
        var storedSession = await _sessionStore.LoadAsync(cancellationToken);
        if (storedSession is null)
        {
            CurrentSession = null;
            return AuthOperationResult.Failure(AuthFailureState.SessionMissing, "No persisted session is available.");
        }

        if (!IsTokenExpired(storedSession.AccessToken))
        {
            CurrentSession = storedSession;
            return AuthOperationResult.Success(storedSession, "Persisted session restored.");
        }

        return await HandleExpiredSessionAsync(storedSession, cancellationToken);
    }

    public async Task<AuthOperationResult> RefreshSessionAsync(CancellationToken cancellationToken = default)
    {
        if (CurrentSession is null)
        {
            return AuthOperationResult.Failure(AuthFailureState.SessionMissing, "No active session is available to refresh.");
        }

        return await RefreshSessionCoreAsync(CurrentSession, cancellationToken);
    }

    public async Task<AuthOperationResult> LogoutAsync(CancellationToken cancellationToken = default)
    {
        CurrentSession = null;
        try
        {
            await _sessionStore.ClearAsync(cancellationToken);
            return AuthOperationResult.Success(session: null, "Session cleared.");
        }
        catch (Exception exception)
        {
            return AuthOperationResult.Failure(
                AuthFailureState.Unknown,
                "Session cleared in memory but persisted session removal failed.",
                exception);
        }
    }

    public bool IsTokenExpired(string accessToken)
    {
        if (!TryGetTokenExpiryUtc(accessToken, out var expiryUtc))
        {
            return true;
        }

        var cutoff = DateTimeOffset.UtcNow.Add(_expiryGracePeriod);
        return expiryUtc <= cutoff;
    }

    public static bool TryGetTokenExpiryUtc(string accessToken, out DateTimeOffset expiryUtc)
    {
        expiryUtc = default;
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var segments = accessToken.Split('.');
        if (segments.Length < 2)
        {
            return false;
        }

        var payload = segments[1]
            .Replace('-', '+')
            .Replace('_', '/');

        switch (payload.Length % 4)
        {
            case 2:
                payload += "==";
                break;
            case 3:
                payload += "=";
                break;
        }

        try
        {
            var bytes = Convert.FromBase64String(payload);
            using var document = JsonDocument.Parse(bytes);
            if (!document.RootElement.TryGetProperty("exp", out var expElement))
            {
                return false;
            }

            if (!expElement.TryGetInt64(out var unixSeconds))
            {
                return false;
            }

            expiryUtc = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private async Task<AuthOperationResult> HandleExpiredSessionAsync(
        DesktopSession expiredSession,
        CancellationToken cancellationToken)
    {
        if (_refreshSessionAsync is null)
        {
            CurrentSession = null;
            await _sessionStore.ClearAsync(cancellationToken);
            return AuthOperationResult.Failure(AuthFailureState.TokenExpired, "Session token has expired.");
        }

        return await RefreshSessionCoreAsync(expiredSession, cancellationToken);
    }

    private async Task<AuthOperationResult> RefreshSessionCoreAsync(
        DesktopSession session,
        CancellationToken cancellationToken)
    {
        if (_refreshSessionAsync is null)
        {
            return AuthOperationResult.Failure(AuthFailureState.RefreshFailed, "Session refresh is not configured.");
        }

        try
        {
            var refreshed = await _refreshSessionAsync(session, cancellationToken);
            if (refreshed is null)
            {
                CurrentSession = null;
                await _sessionStore.ClearAsync(cancellationToken);
                return AuthOperationResult.Failure(AuthFailureState.RefreshFailed, "Session refresh did not produce a valid token.");
            }

            await _sessionStore.SaveAsync(refreshed, cancellationToken);
            CurrentSession = refreshed;
            return AuthOperationResult.Success(refreshed, "Session refreshed.");
        }
        catch (Exception exception)
        {
            CurrentSession = null;
            await _sessionStore.ClearAsync(cancellationToken);
            var result = MapFailureResult(exception, fallbackMessage: "Session refresh failed.");
            return result with { FailureState = AuthFailureState.RefreshFailed };
        }
    }

    private static AuthOperationResult MapFailureResult(Exception exception, string fallbackMessage)
    {
        return exception switch
        {
            ApiClientException { StatusCode: HttpStatusCode.BadRequest } apiException
                => AuthOperationResult.Failure(AuthFailureState.InvalidCredentials, apiException.Message, apiException),
            ApiClientException { StatusCode: HttpStatusCode.Forbidden } apiException
                => AuthOperationResult.Failure(AuthFailureState.Forbidden, apiException.Message, apiException),
            ApiClientException { StatusCode: HttpStatusCode.Unauthorized } apiException
                => AuthOperationResult.Failure(AuthFailureState.Unauthorized, apiException.Message, apiException),
            ApiClientException apiException when (int)apiException.StatusCode >= 500
                => AuthOperationResult.Failure(AuthFailureState.BackendError, apiException.Message, apiException),
            HttpRequestException requestException
                => AuthOperationResult.Failure(AuthFailureState.NetworkError, requestException.Message, requestException),
            _ => AuthOperationResult.Failure(AuthFailureState.Unknown, fallbackMessage, exception)
        };
    }
}
