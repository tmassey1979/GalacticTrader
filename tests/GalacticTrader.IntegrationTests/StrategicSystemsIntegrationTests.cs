namespace GalacticTrader.IntegrationTests;

using System.Net;
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
    public async Task StrategicPhaseOne_EndpointsAreWired()
    {
        var listVolatility = await _client.GetAsync("/api/strategic/volatility");
        Assert.Equal(HttpStatusCode.OK, listVolatility.StatusCode);

        var upsertVolatility = await _client.PostAsJsonAsync("/api/strategic/volatility", new
        {
            sectorId = Guid.NewGuid(),
            currentPhase = "volatile",
            volatilityIndex = 55f,
            nextTransitionAt = DateTime.UtcNow.AddHours(4)
        });
        Assert.Equal(HttpStatusCode.NotFound, upsertVolatility.StatusCode);

        var sameFaction = Guid.NewGuid();
        var declareWar = await _client.PostAsJsonAsync("/api/strategic/corporate-wars", new
        {
            attackerFactionId = sameFaction,
            defenderFactionId = sameFaction,
            casusBelli = "invalid",
            intensity = 20
        });
        Assert.Equal(HttpStatusCode.BadRequest, declareWar.StatusCode);

        var listWars = await _client.GetAsync("/api/strategic/corporate-wars?activeOnly=true");
        Assert.Equal(HttpStatusCode.OK, listWars.StatusCode);

        var listInfrastructure = await _client.GetAsync("/api/strategic/infrastructure");
        Assert.Equal(HttpStatusCode.OK, listInfrastructure.StatusCode);

        var recalcUnknownFaction = await _client.PostAsync($"/api/strategic/territory-dominance/recalculate/{Guid.NewGuid()}", null);
        Assert.Equal(HttpStatusCode.NotFound, recalcUnknownFaction.StatusCode);

        var listDominance = await _client.GetAsync("/api/strategic/territory-dominance");
        Assert.Equal(HttpStatusCode.OK, listDominance.StatusCode);
    }

    [Fact]
    public async Task StrategicPhaseTwo_EndpointsAreWired()
    {
        var playerId = Guid.NewGuid();
        var shipId = Guid.NewGuid();
        var sectorId = Guid.NewGuid();
        var networkId = Guid.NewGuid();

        var listPolicies = await _client.GetAsync($"/api/strategic/insurance/policies/{playerId}");
        Assert.Equal(HttpStatusCode.OK, listPolicies.StatusCode);

        var upsertPolicy = await _client.PostAsJsonAsync("/api/strategic/insurance/policies", new
        {
            playerId,
            shipId,
            coverageRate = 0.75f,
            premiumPerCycle = 300m,
            riskTier = "standard",
            isActive = true
        });
        Assert.Equal(HttpStatusCode.NotFound, upsertPolicy.StatusCode);

        var listClaims = await _client.GetAsync($"/api/strategic/insurance/claims/{playerId}");
        Assert.Equal(HttpStatusCode.OK, listClaims.StatusCode);

        var fileClaim = await _client.PostAsJsonAsync("/api/strategic/insurance/claims", new
        {
            policyId = Guid.NewGuid(),
            combatLogId = Guid.NewGuid(),
            claimAmount = 1500m
        });
        Assert.Equal(HttpStatusCode.NotFound, fileClaim.StatusCode);

        var createNetwork = await _client.PostAsJsonAsync("/api/strategic/intelligence/networks", new
        {
            ownerPlayerId = playerId,
            name = "test-network",
            assetCount = 8,
            coverageScore = 52f
        });
        Assert.Equal(HttpStatusCode.NotFound, createNetwork.StatusCode);

        var publishReport = await _client.PostAsJsonAsync("/api/strategic/intelligence/reports", new
        {
            networkId,
            sectorId,
            signalType = "pirate-sighting",
            confidenceScore = 80f,
            payload = "Detected heavy traffic",
            ttlMinutes = 30
        });
        Assert.Equal(HttpStatusCode.NotFound, publishReport.StatusCode);

        var listReports = await _client.GetAsync($"/api/strategic/intelligence/reports/{playerId}");
        Assert.Equal(HttpStatusCode.OK, listReports.StatusCode);

        var expireReports = await _client.PostAsync("/api/strategic/intelligence/reports/expire", null);
        Assert.Equal(HttpStatusCode.OK, expireReports.StatusCode);
    }
}
