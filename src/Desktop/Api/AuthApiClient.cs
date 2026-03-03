using System.Net.Http;
using System.Net.Http.Json;

namespace GalacticTrader.Desktop.Api;

public sealed class AuthApiClient
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

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Register failed ({(int)response.StatusCode}): {detail}");
        }
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

        if (!response.IsSuccessStatusCode)
        {
            var detail = await response.Content.ReadAsStringAsync(cancellationToken);
            
            throw new InvalidOperationException($"Login failed ({(int)response.StatusCode}): {detail}");
        }

        var payload = await response.Content.ReadFromJsonAsync<AuthLoginResultDto>(cancellationToken);
        if (payload is null || payload.Player is null || string.IsNullOrWhiteSpace(payload.AccessToken))
        {
            throw new InvalidOperationException("Login response did not include player session details.");
        }

        return new DesktopSession(payload.Player.PlayerId, payload.Player.Username, payload.AccessToken);
    }
}
