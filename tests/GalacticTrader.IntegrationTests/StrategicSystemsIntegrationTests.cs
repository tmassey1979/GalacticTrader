namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

public sealed class StrategicSystemsIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public StrategicSystemsIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task StrategicReadEndpoints_AreWired()
    {
        var listVolatility = await _client.GetAsync("/api/strategic/volatility");
        Assert.Equal(HttpStatusCode.OK, listVolatility.StatusCode);

        var listWars = await _client.GetAsync("/api/strategic/corporate-wars?activeOnly=true");
        Assert.Equal(HttpStatusCode.OK, listWars.StatusCode);

        var listInfrastructure = await _client.GetAsync("/api/strategic/infrastructure");
        Assert.Equal(HttpStatusCode.OK, listInfrastructure.StatusCode);

        var listDominance = await _client.GetAsync("/api/strategic/territory-dominance");
        Assert.Equal(HttpStatusCode.OK, listDominance.StatusCode);

        var listPolicies = await _client.GetAsync($"/api/strategic/insurance/policies/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, listPolicies.StatusCode);

        var listClaims = await _client.GetAsync($"/api/strategic/insurance/claims/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, listClaims.StatusCode);

        var listReports = await _client.GetAsync($"/api/strategic/intelligence/reports/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.OK, listReports.StatusCode);
    }

    [Fact]
    public async Task StrategicMutationEndpoints_RequireAuthorization()
    {
        var upsertVolatility = await _client.PostAsJsonAsync("/api/strategic/volatility", new
        {
            sectorId = Guid.NewGuid(),
            currentPhase = "volatile",
            volatilityIndex = 55f,
            nextTransitionAt = DateTime.UtcNow.AddHours(4)
        });
        Assert.Equal(HttpStatusCode.Unauthorized, upsertVolatility.StatusCode);

        var declareWar = await _client.PostAsJsonAsync("/api/strategic/corporate-wars", new
        {
            attackerFactionId = Guid.NewGuid(),
            defenderFactionId = Guid.NewGuid(),
            casusBelli = "test",
            intensity = 20
        });
        Assert.Equal(HttpStatusCode.Unauthorized, declareWar.StatusCode);

        var upsertInfrastructure = await _client.PostAsJsonAsync("/api/strategic/infrastructure", new
        {
            sectorId = Guid.NewGuid(),
            factionId = Guid.NewGuid(),
            infrastructureType = "station",
            controlScore = 75f
        });
        Assert.Equal(HttpStatusCode.Unauthorized, upsertInfrastructure.StatusCode);

        var recalcUnknownFaction = await _client.PostAsync($"/api/strategic/territory-dominance/recalculate/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.Unauthorized, recalcUnknownFaction.StatusCode);

        var upsertPolicy = await _client.PostAsJsonAsync("/api/strategic/insurance/policies", new
        {
            playerId = Guid.NewGuid(),
            shipId = Guid.NewGuid(),
            coverageRate = 0.75f,
            premiumPerCycle = 300m,
            riskTier = "standard",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.Unauthorized, upsertPolicy.StatusCode);

        var fileClaim = await _client.PostAsJsonAsync("/api/strategic/insurance/claims", new
        {
            policyId = Guid.NewGuid(),
            combatLogId = Guid.NewGuid(),
            claimAmount = 1500m
        });
        Assert.Equal(HttpStatusCode.Unauthorized, fileClaim.StatusCode);

        var createNetwork = await _client.PostAsJsonAsync("/api/strategic/intelligence/networks", new
        {
            ownerPlayerId = Guid.NewGuid(),
            name = "test-network",
            assetCount = 8,
            coverageScore = 52f
        });
        Assert.Equal(HttpStatusCode.Unauthorized, createNetwork.StatusCode);

        var publishReport = await _client.PostAsJsonAsync("/api/strategic/intelligence/reports", new
        {
            networkId = Guid.NewGuid(),
            sectorId = Guid.NewGuid(),
            signalType = "pirate-sighting",
            confidenceScore = 80f,
            payload = "Detected heavy traffic",
            ttlMinutes = 30
        });
        Assert.Equal(HttpStatusCode.Unauthorized, publishReport.StatusCode);

        var expireReports = await _client.PostAsync("/api/strategic/intelligence/reports/expire", null);
        Assert.Equal(HttpStatusCode.Unauthorized, expireReports.StatusCode);
    }

    [Fact]
    public async Task StrategicAdminMutation_RequiresAdminRole()
    {
        var username = $"strategic_user_{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });

        var nonAdminToken = await LoginAndGetTokenAsync(username, "WarpDrive123!");
        var adminToken = await LoginAndGetTokenAsync("viper", "ViperDev123!");

        var nonAdminResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/reports/expire",
            nonAdminToken);
        Assert.Equal(HttpStatusCode.Forbidden, nonAdminResponse.StatusCode);

        var adminResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/reports/expire",
            adminToken);
        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
    }

    [Fact]
    public async Task StrategicOwnershipMutations_RequireOwnerOrAdmin()
    {
        var owner = await RegisterAndLoginAsync("owner");
        var intruder = await RegisterAndLoginAsync("intruder");
        var adminToken = await LoginAndGetTokenAsync("viper", "ViperDev123!");

        var createNetworkResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/networks",
            owner.AccessToken,
            new
            {
                ownerPlayerId = owner.PlayerId,
                name = $"owner-net-{Guid.NewGuid():N}"[..20],
                assetCount = 8,
                coverageScore = 52f
            });
        createNetworkResponse.EnsureSuccessStatusCode();
        var network = await createNetworkResponse.Content.ReadFromJsonAsync<IntelligenceNetworkResponse>();
        Assert.NotNull(network);

        var intruderResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/reports",
            intruder.AccessToken,
            new
            {
                networkId = network!.Id,
                sectorId = Guid.NewGuid(),
                signalType = "pirate-sighting",
                confidenceScore = 80f,
                payload = "Detected heavy traffic",
                ttlMinutes = 30
            });
        Assert.Equal(HttpStatusCode.Forbidden, intruderResponse.StatusCode);

        var ownerResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/reports",
            owner.AccessToken,
            new
            {
                networkId = network.Id,
                sectorId = Guid.NewGuid(),
                signalType = "pirate-sighting",
                confidenceScore = 80f,
                payload = "Detected heavy traffic",
                ttlMinutes = 30
            });
        Assert.Equal(HttpStatusCode.NotFound, ownerResponse.StatusCode);

        var adminResponse = await SendWithBearerTokenAsync(
            HttpMethod.Post,
            "/api/strategic/intelligence/reports",
            adminToken,
            new
            {
                networkId = network.Id,
                sectorId = Guid.NewGuid(),
                signalType = "pirate-sighting",
                confidenceScore = 80f,
                payload = "Detected heavy traffic",
                ttlMinutes = 30
            });
        Assert.Equal(HttpStatusCode.NotFound, adminResponse.StatusCode);
    }

    private async Task<(Guid PlayerId, string AccessToken)> RegisterAndLoginAsync(string prefix)
    {
        var username = $"{prefix}_{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });

        var login = await LoginAsync(username, "WarpDrive123!");
        return (login.Player.PlayerId, login.AccessToken);
    }

    private async Task<string> LoginAndGetTokenAsync(string username, string password)
    {
        var payload = await LoginAsync(username, password);
        return payload.AccessToken;
    }

    private async Task<LoginResponse> LoginAsync(string username, string password)
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
        return payload;
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
    private sealed record IntelligenceNetworkResponse(Guid Id);
}
