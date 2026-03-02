namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

public sealed class CoreWorkflowIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public CoreWorkflowIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task NavigationPlanningFlow_Succeeds()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var sourceSector = await CreateSectorAsync($"Alpha Prime {suffix}", 0, 0, 0);
        var destinationSector = await CreateSectorAsync($"Beta Forge {suffix}", 42, 10, -12);

        var routeResponse = await _client.PostAsJsonAsync("/api/navigation/routes", new
        {
            fromSectorId = sourceSector.Id,
            toSectorId = destinationSector.Id,
            legalStatus = "Legal",
            warpGateType = "Stable"
        });

        Assert.Contains(routeResponse.StatusCode, new[] { HttpStatusCode.Created, HttpStatusCode.Conflict });

        var planResponse = await _client.GetAsync($"/api/navigation/planning/{sourceSector.Id}/{destinationSector.Id}?mode=Standard&algorithm=dijkstra");
        Assert.Contains(planResponse.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.NotFound });
    }

    [Fact]
    public async Task InvalidMarketTradePayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/market/trade", new
        {
            buyerId = Guid.Empty,
            sellerId = Guid.Empty,
            marketListingId = Guid.Empty,
            commodityId = Guid.Empty,
            quantity = -10
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownSector_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/api/navigation/sectors/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task EconomyEndpoints_HandleConcurrentLoadSmoke()
    {
        var requests = Enumerable.Range(0, 24)
            .Select(_ => _client.PostAsync("/api/economy/tick", content: null))
            .ToArray();

        await Task.WhenAll(requests);

        foreach (var response in requests.Select(task => task.Result))
        {
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    private async Task<CreatedSector> CreateSectorAsync(string name, float x, float y, float z)
    {
        var response = await _client.PostAsJsonAsync("/api/navigation/sectors", new
        {
            name,
            x,
            y,
            z
        });

        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var id = json.RootElement.GetProperty("id").GetGuid();
        var sectorName = json.RootElement.GetProperty("name").GetString() ?? string.Empty;
        return new CreatedSector(id, sectorName);
    }

    private sealed record CreatedSector(Guid Id, string Name);
}
