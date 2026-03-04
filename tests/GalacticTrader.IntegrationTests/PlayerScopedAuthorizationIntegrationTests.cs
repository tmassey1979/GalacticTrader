namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public sealed class PlayerScopedAuthorizationIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public PlayerScopedAuthorizationIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task MarketFleetAndReputationPlayerScopedReads_RequireOwnerOrAdmin()
    {
        var owner = await RegisterAndLoginAsync("scoped-owner");
        var intruder = await RegisterAndLoginAsync("scoped-intr");
        var adminToken = await LoginAndGetTokenAsync("viper", "ViperDev123!");

        var restrictedEndpoints = new[]
        {
            $"/api/market/transactions/{owner.PlayerId:D}",
            $"/api/fleet/players/{owner.PlayerId:D}/ships",
            $"/api/fleet/players/{owner.PlayerId:D}/escort",
            $"/api/reputation/factions/{owner.PlayerId:D}",
            $"/api/reputation/factions/{owner.PlayerId:D}/benefits",
            $"/api/reputation/alignment/{owner.PlayerId:D}"
        };

        foreach (var endpoint in restrictedEndpoints)
        {
            var noToken = await _client.GetAsync(endpoint);
            Assert.Equal(HttpStatusCode.Unauthorized, noToken.StatusCode);

            var intruderAttempt = await SendWithBearerTokenAsync(HttpMethod.Get, endpoint, intruder.AccessToken);
            Assert.Equal(HttpStatusCode.Forbidden, intruderAttempt.StatusCode);

            var ownerAttempt = await SendWithBearerTokenAsync(HttpMethod.Get, endpoint, owner.AccessToken);
            Assert.Equal(HttpStatusCode.OK, ownerAttempt.StatusCode);

            var adminAttempt = await SendWithBearerTokenAsync(HttpMethod.Get, endpoint, adminToken);
            Assert.Equal(HttpStatusCode.OK, adminAttempt.StatusCode);
        }
    }

    [Fact]
    public async Task LeaderboardPlayerScopedReads_ArePublic()
    {
        var owner = await RegisterAndLoginAsync("leader-owner");
        var intruder = await RegisterAndLoginAsync("leader-intr");
        var adminToken = await LoginAndGetTokenAsync("viper", "ViperDev123!");

        var recalculate = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/leaderboards/recalculate",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, recalculate.StatusCode);

        var positionEndpoint = $"/api/leaderboards/wealth/player/{owner.PlayerId:D}";
        var historyEndpoint = $"/api/leaderboards/wealth/player/{owner.PlayerId:D}/history?limit=10";

        var positionNoToken = await _client.GetAsync(positionEndpoint);
        Assert.Equal(HttpStatusCode.OK, positionNoToken.StatusCode);

        var positionIntruder = await SendWithBearerTokenAsync(HttpMethod.Get, positionEndpoint, intruder.AccessToken);
        Assert.Equal(HttpStatusCode.OK, positionIntruder.StatusCode);

        var historyNoToken = await _client.GetAsync(historyEndpoint);
        Assert.Equal(HttpStatusCode.OK, historyNoToken.StatusCode);

        var historyIntruder = await SendWithBearerTokenAsync(HttpMethod.Get, historyEndpoint, intruder.AccessToken);
        Assert.Equal(HttpStatusCode.OK, historyIntruder.StatusCode);
    }

    private async Task<(Guid PlayerId, string AccessToken)> RegisterAndLoginAsync(string usernamePrefix)
    {
        var username = $"{usernamePrefix}_{Guid.NewGuid():N}"[..20];
        var register = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Created, register.StatusCode);

        var login = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "WarpDrive123!"
        });
        login.EnsureSuccessStatusCode();
        var payload = await login.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        return (payload.Player.PlayerId, payload.AccessToken);
    }

    private async Task<string> LoginAndGetTokenAsync(string username, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password
        });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
        return payload.AccessToken;
    }

    private Task<HttpResponseMessage> SendWithBearerTokenAsync(
        HttpMethod method,
        string path,
        string accessToken,
        object? payload = null)
    {
        var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (payload is not null)
        {
            request.Content = JsonContent.Create(payload);
        }

        return _client.SendAsync(request);
    }

    private sealed record LoginResponse(LoginPlayer Player, string AccessToken);
    private sealed record LoginPlayer(Guid PlayerId, string Username, string Email);
}
