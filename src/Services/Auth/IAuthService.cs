namespace GalacticTrader.Services.Auth;

public interface IAuthService
{
    Task<PlayerIdentity> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult?> LoginAsync(LoginPlayerRequest request, CancellationToken cancellationToken = default);

    Task<PlayerSession?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}

public sealed record RegisterPlayerRequest(string Username, string Email, string Password);

public sealed record LoginPlayerRequest(string Username, string Password);

public sealed record PlayerIdentity(Guid PlayerId, string Username, string Email, DateTimeOffset RegisteredAtUtc);

public sealed record LoginResult(PlayerIdentity Player, string AccessToken, DateTimeOffset ExpiresAtUtc);

public sealed record PlayerSession(PlayerIdentity Player, string AccessToken, DateTimeOffset ExpiresAtUtc);
