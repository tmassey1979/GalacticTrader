namespace GalacticTrader.Desktop.Api;

public interface IAuthApiClient
{
    Task RegisterAsync(RegisterPlayerRequestDto request, CancellationToken cancellationToken = default);

    Task RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default);

    Task<DesktopSession> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
}
