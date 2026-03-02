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
}
