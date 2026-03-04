using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class AuthApiClient : IAuthApiClient
{
    private readonly HttpClient _httpClient;

    public AuthApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task RegisterAsync(RegisterPlayerRequestDto request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/register",
            request,
            cancellationToken);

        await ApiClientRuntime.EnsureSuccessAsync(response, "Register failed", cancellationToken);
    }

    public Task RegisterAsync(string username, string email, string password, CancellationToken cancellationToken = default)
    {
        return RegisterAsync(new RegisterPlayerRequestDto(username, email, password), cancellationToken);
    }

    public async Task<DesktopSession> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync(
            "/api/auth/login",
            new
            {
                username,
                password
            },
            cancellationToken);

        await ApiClientRuntime.EnsureSuccessAsync(response, "Login failed", cancellationToken);

        var payload = await ApiClientRuntime.ReadAsync<AuthLoginResultDto>(response, cancellationToken);
        if (payload is null || payload.Player is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Login response did not include player session details.");
        }

        return new DesktopSession(payload.Player.PlayerId, payload.Player.Username, payload.AccessToken);
    }
}



