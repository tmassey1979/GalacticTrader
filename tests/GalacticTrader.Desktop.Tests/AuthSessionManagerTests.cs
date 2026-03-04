using GalacticTrader.Desktop.Api;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GalacticTrader.Desktop.Tests;

public sealed class AuthSessionManagerTests
{
    [Fact]
    public async Task LoginAsync_PersistsSession_WhenCredentialsAreValid()
    {
        var expectedSession = new DesktopSession(Guid.NewGuid(), "viper", BuildJwtToken(DateTimeOffset.UtcNow.AddHours(1)));
        var authClient = new StubAuthApiClient
        {
            LoginHandler = (_, _, _) => Task.FromResult(expectedSession)
        };
        var store = new InMemoryClientSessionStore();
        var manager = new AuthSessionManager(authClient, store);

        var result = await manager.LoginAsync("viper", "secret");
        var persisted = await store.LoadAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(AuthFailureState.None, result.FailureState);
        Assert.Equal(expectedSession, result.Session);
        Assert.Equal(expectedSession, persisted);
        Assert.Equal(expectedSession, manager.CurrentSession);
    }

    [Fact]
    public async Task LoginAsync_ReturnsFailureState_WhenCredentialsAreInvalid()
    {
        var authClient = new StubAuthApiClient
        {
            LoginHandler = (_, _, _) => Task.FromException<DesktopSession>(
                new ApiClientException("Login failed", HttpStatusCode.BadRequest, "invalid credentials"))
        };
        var store = new InMemoryClientSessionStore();
        var manager = new AuthSessionManager(authClient, store);

        var result = await manager.LoginAsync("viper", "bad-password");

        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureState.InvalidCredentials, result.FailureState);
        Assert.Null(result.Session);
        Assert.Null(manager.CurrentSession);
    }

    [Fact]
    public async Task RestoreSessionAsync_RestoresActivePersistedSession()
    {
        var store = new InMemoryClientSessionStore();
        var activeSession = new DesktopSession(Guid.NewGuid(), "viper", BuildJwtToken(DateTimeOffset.UtcNow.AddMinutes(10)));
        await store.SaveAsync(activeSession);

        var manager = new AuthSessionManager(new StubAuthApiClient(), store);
        var result = await manager.RestoreSessionAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(activeSession, result.Session);
        Assert.Equal(activeSession, manager.CurrentSession);
    }

    [Fact]
    public async Task RestoreSessionAsync_RefreshesExpiredSession_WhenRefreshDelegateConfigured()
    {
        var expired = new DesktopSession(Guid.NewGuid(), "viper", BuildJwtToken(DateTimeOffset.UtcNow.AddMinutes(-5)));
        var refreshed = expired with { AccessToken = BuildJwtToken(DateTimeOffset.UtcNow.AddHours(1)) };
        var store = new InMemoryClientSessionStore();
        await store.SaveAsync(expired);

        var manager = new AuthSessionManager(
            new StubAuthApiClient(),
            store,
            refreshSessionAsync: (session, _) => Task.FromResult<DesktopSession?>(session with
            {
                AccessToken = refreshed.AccessToken
            }));

        var result = await manager.RestoreSessionAsync();
        var persisted = await store.LoadAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(refreshed.AccessToken, result.Session?.AccessToken);
        Assert.Equal(refreshed.AccessToken, persisted?.AccessToken);
        Assert.Equal(refreshed.AccessToken, manager.CurrentSession?.AccessToken);
    }

    [Fact]
    public async Task RestoreSessionAsync_ClearsExpiredSession_WhenRefreshIsUnavailable()
    {
        var expired = new DesktopSession(Guid.NewGuid(), "viper", BuildJwtToken(DateTimeOffset.UtcNow.AddMinutes(-5)));
        var store = new InMemoryClientSessionStore();
        await store.SaveAsync(expired);

        var manager = new AuthSessionManager(new StubAuthApiClient(), store);
        var result = await manager.RestoreSessionAsync();
        var persisted = await store.LoadAsync();

        Assert.False(result.Succeeded);
        Assert.Equal(AuthFailureState.TokenExpired, result.FailureState);
        Assert.Null(persisted);
        Assert.Null(manager.CurrentSession);
    }

    [Fact]
    public async Task LogoutAsync_ClearsInMemoryAndPersistedSessionDeterministically()
    {
        var session = new DesktopSession(Guid.NewGuid(), "viper", BuildJwtToken(DateTimeOffset.UtcNow.AddMinutes(30)));
        var store = new InMemoryClientSessionStore();
        await store.SaveAsync(session);

        var manager = new AuthSessionManager(new StubAuthApiClient(), store);
        await manager.RestoreSessionAsync();

        var result = await manager.LogoutAsync();
        var persisted = await store.LoadAsync();

        Assert.True(result.Succeeded);
        Assert.Equal(AuthFailureState.None, result.FailureState);
        Assert.Null(persisted);
        Assert.Null(manager.CurrentSession);
    }

    [Fact]
    public void TryGetTokenExpiryUtc_ReturnsFalse_ForMalformedToken()
    {
        var parsed = AuthSessionManager.TryGetTokenExpiryUtc("not-a-token", out var expires);

        Assert.False(parsed);
        Assert.Equal(default, expires);
    }

    private static string BuildJwtToken(DateTimeOffset expiresUtc)
    {
        var headerJson = JsonSerializer.Serialize(new { alg = "none", typ = "JWT" });
        var payloadJson = JsonSerializer.Serialize(new { exp = expiresUtc.ToUnixTimeSeconds() });

        var header = Base64UrlEncode(headerJson);
        var payload = Base64UrlEncode(payloadJson);
        return $"{header}.{payload}.signature";
    }

    private static string Base64UrlEncode(string value)
    {
        return Convert
            .ToBase64String(Encoding.UTF8.GetBytes(value))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private sealed class StubAuthApiClient : IAuthApiClient
    {
        public Func<string, string, CancellationToken, Task<DesktopSession>>? LoginHandler { get; init; }

        public Task RegisterAsync(RegisterPlayerRequestDto request, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<DesktopSession> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
        {
            if (LoginHandler is null)
            {
                throw new InvalidOperationException("Login handler not configured.");
            }

            return LoginHandler(username, password, cancellationToken);
        }
    }
}
