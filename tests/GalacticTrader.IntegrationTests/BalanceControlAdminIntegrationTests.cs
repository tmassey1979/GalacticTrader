namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

public sealed class BalanceControlAdminIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string AdminKey = "dev-admin-key";

    public BalanceControlAdminIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task BalanceState_RejectsRequestsWithoutAdminKey()
    {
        var response = await _client.GetAsync("/api/admin/balance/state");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BalanceEndpoints_WithAdminKey_UpdateAndReturnState()
    {
        var sectorId = Guid.NewGuid();

        var taxResponse = await PostWithAdminKeyAsync("/api/admin/balance/tax", new { taxRatePercent = 7.5m });
        Assert.Equal(HttpStatusCode.OK, taxResponse.StatusCode);

        var pirateResponse = await PostWithAdminKeyAsync("/api/admin/balance/pirates", new { intensityPercent = 63 });
        Assert.Equal(HttpStatusCode.OK, pirateResponse.StatusCode);

        var liquidityResponse = await PostWithAdminKeyAsync("/api/admin/balance/liquidity", new
        {
            deltaPercent = -4.25m,
            reason = "stability-test"
        });
        Assert.Equal(HttpStatusCode.OK, liquidityResponse.StatusCode);

        var instabilityResponse = await PostWithAdminKeyAsync("/api/admin/balance/instability", new
        {
            sectorId,
            reason = "drill"
        });
        Assert.Equal(HttpStatusCode.OK, instabilityResponse.StatusCode);

        var correctionResponse = await PostWithAdminKeyAsync("/api/admin/balance/correction", new
        {
            adjustmentPercent = -12.5m,
            reason = "market-reset"
        });
        Assert.Equal(HttpStatusCode.OK, correctionResponse.StatusCode);

        var stateResponse = await GetWithAdminKeyAsync("/api/admin/balance/state");
        stateResponse.EnsureSuccessStatusCode();

        var state = await stateResponse.Content.ReadFromJsonAsync<BalanceControlStateDto>();
        Assert.NotNull(state);
        Assert.Equal(7.5m, state.TaxRatePercent);
        Assert.Equal(63, state.PirateIntensityPercent);
        Assert.Equal(-4.25m, state.LiquidityAdjustmentPercent);
        Assert.Equal(-12.5m, state.EconomicCorrectionPercent);
        Assert.Contains(sectorId, state.UnstableSectors);
    }

    private Task<HttpResponseMessage> GetWithAdminKeyAsync(string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Admin-Key", AdminKey);
        return _client.SendAsync(request);
    }

    private Task<HttpResponseMessage> PostWithAdminKeyAsync(string path, object payload)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = JsonContent.Create(payload)
        };
        request.Headers.Add("X-Admin-Key", AdminKey);
        return _client.SendAsync(request);
    }

    private sealed class BalanceControlStateDto
    {
        public decimal TaxRatePercent { get; init; }
        public int PirateIntensityPercent { get; init; }
        public decimal LiquidityAdjustmentPercent { get; init; }
        public decimal EconomicCorrectionPercent { get; init; }
        public IReadOnlyList<Guid> UnstableSectors { get; init; } = [];
    }
}
