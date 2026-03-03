namespace GalacticTrader.Services.Auth;

public interface IAuthService
{
    Task<PlayerIdentity> RegisterAsync(RegisterPlayerRequest request, CancellationToken cancellationToken = default);

    Task<LoginResult?> LoginAsync(LoginPlayerRequest request, CancellationToken cancellationToken = default);

    Task<PlayerSession?> ValidateTokenAsync(string accessToken, CancellationToken cancellationToken = default);
}
