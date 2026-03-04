namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;

public sealed class BalanceControlAdminIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly ApiWebApplicationFactory _factory;
    private const string AdminKey = "dev-admin-key";

    public BalanceControlAdminIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task BalanceState_RejectsRequestsWithoutAuthorization()
    {
        var response = await _client.GetAsync("/api/admin/balance/state");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BalanceEndpoints_WithAdminBearerToken_UpdateAndReturnState()
    {
        var adminToken = await LoginAndGetTokenAsync(_client, "viper", "ViperDev123!");
        var sectorId = Guid.NewGuid();

        var taxResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Post, "/api/admin/balance/tax", adminToken, new { taxRatePercent = 7.5m });
        Assert.Equal(HttpStatusCode.OK, taxResponse.StatusCode);

        var pirateResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Post, "/api/admin/balance/pirates", adminToken, new { intensityPercent = 63 });
        Assert.Equal(HttpStatusCode.OK, pirateResponse.StatusCode);

        var liquidityResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Post, "/api/admin/balance/liquidity", adminToken, new
        {
            deltaPercent = -4.25m,
            reason = "stability-test"
        });
        Assert.Equal(HttpStatusCode.OK, liquidityResponse.StatusCode);

        var instabilityResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Post, "/api/admin/balance/instability", adminToken, new
        {
            sectorId,
            reason = "drill"
        });
        Assert.Equal(HttpStatusCode.OK, instabilityResponse.StatusCode);

        var correctionResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Post, "/api/admin/balance/correction", adminToken, new
        {
            adjustmentPercent = -12.5m,
            reason = "market-reset"
        });
        Assert.Equal(HttpStatusCode.OK, correctionResponse.StatusCode);

        var stateResponse = await SendWithBearerTokenAsync(_client, HttpMethod.Get, "/api/admin/balance/state", adminToken);
        stateResponse.EnsureSuccessStatusCode();

        var state = await stateResponse.Content.ReadFromJsonAsync<BalanceControlStateDto>();
        Assert.NotNull(state);
        Assert.Equal(7.5m, state.TaxRatePercent);
        Assert.Equal(63, state.PirateIntensityPercent);
        Assert.Equal(-4.25m, state.LiquidityAdjustmentPercent);
        Assert.Equal(-12.5m, state.EconomicCorrectionPercent);
        Assert.Contains(sectorId, state.UnstableSectors);
    }

    [Fact]
    public async Task BalanceState_WithNonAdminBearerToken_ReturnsForbidden()
    {
        var username = $"balance_user_{Guid.NewGuid():N}"[..20];
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });

        var token = await LoginAndGetTokenAsync(_client, username, "WarpDrive123!");
        var response = await SendWithBearerTokenAsync(_client, HttpMethod.Get, "/api/admin/balance/state", token);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task LegacyAdminKey_WorksWhenMigrationFlagEnabled()
    {
        using var client = CreateClientWithLegacyKeyAuth(enabled: true, includeAdminKeyConfiguration: true);
        var response = await GetWithAdminKeyAsync(client, "/api/admin/balance/state");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task LegacyAdminKey_IsRejectedWhenMigrationFlagDisabled()
    {
        using var client = CreateClientWithLegacyKeyAuth(enabled: false, includeAdminKeyConfiguration: true);
        var response = await GetWithAdminKeyAsync(client, "/api/admin/balance/state");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task LegacyAdminKey_IsRejectedWhenAdminKeyNotConfigured()
    {
        using var client = CreateClientWithLegacyKeyAuth(enabled: true, includeAdminKeyConfiguration: false);
        var response = await GetWithAdminKeyAsync(client, "/api/admin/balance/state");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private HttpClient CreateClientWithLegacyKeyAuth(bool enabled, bool includeAdminKeyConfiguration)
    {
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                var values = new Dictionary<string, string?>
                {
                    ["Admin:AllowLegacyKeyAuth"] = enabled.ToString()
                };
                if (includeAdminKeyConfiguration)
                {
                    values["Admin:Key"] = AdminKey;
                }

                configurationBuilder.AddInMemoryCollection(values);
            });
        });

        return configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    private static async Task<string> LoginAndGetTokenAsync(HttpClient client, string username, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new
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

    private static Task<HttpResponseMessage> SendWithBearerTokenAsync(
        HttpClient client,
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

        return client.SendAsync(request);
    }

    private static Task<HttpResponseMessage> GetWithAdminKeyAsync(HttpClient client, string path)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Add("X-Admin-Key", AdminKey);
        return client.SendAsync(request);
    }

    private sealed class BalanceControlStateDto
    {
        public decimal TaxRatePercent { get; init; }
        public int PirateIntensityPercent { get; init; }
        public decimal LiquidityAdjustmentPercent { get; init; }
        public decimal EconomicCorrectionPercent { get; init; }
        public IReadOnlyList<Guid> UnstableSectors { get; init; } = [];
    }

    private sealed record LoginResponse(string AccessToken);
}
