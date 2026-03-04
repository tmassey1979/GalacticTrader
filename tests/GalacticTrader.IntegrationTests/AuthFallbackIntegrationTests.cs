namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

public sealed class AuthFallbackIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthFallbackIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_UsesLocalAuth_WhenKeycloakIsNotConfigured()
    {
        using var client = _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var username = $"localauth_{Guid.NewGuid():N}"[..20];
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    [Fact]
    public async Task Login_FallsBackToLocalAuth_WhenKeycloakRejectsCredentials_AndFallbackEnabled()
    {
        await using var keycloak = await FakeUnauthorizedKeycloakServer.StartAsync();
        using var client = CreateClientWithKeycloakFallbackPolicy(
            keycloak.BaseUrl,
            allowLocalFallbackOnInvalidCredentials: true);

        var username = $"fallbackok_{Guid.NewGuid():N}"[..20];
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        var payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(payload);
        Assert.False(string.IsNullOrWhiteSpace(payload!.AccessToken));
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenKeycloakRejectsCredentials_AndFallbackDisabled()
    {
        await using var keycloak = await FakeUnauthorizedKeycloakServer.StartAsync();
        using var client = CreateClientWithKeycloakFallbackPolicy(
            keycloak.BaseUrl,
            allowLocalFallbackOnInvalidCredentials: false);

        var username = $"fallbackno_{Guid.NewGuid():N}"[..20];
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", new
        {
            username,
            email = $"{username}@gt.test",
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            username,
            password = "WarpDrive123!"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    private HttpClient CreateClientWithKeycloakFallbackPolicy(
        string keycloakBaseUrl,
        bool allowLocalFallbackOnInvalidCredentials)
    {
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Keycloak:ServerUrl"] = keycloakBaseUrl,
                    ["Keycloak:Realm"] = "test-realm",
                    ["Keycloak:ClientId"] = "test-client",
                    ["Keycloak:RequireHttpsMetadata"] = "false",
                    ["Keycloak:AllowLocalFallbackOnInvalidCredentials"] = allowLocalFallbackOnInvalidCredentials.ToString()
                });
            });
        });

        return configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    private sealed class FakeUnauthorizedKeycloakServer : IAsyncDisposable
    {
        private readonly WebApplication _app;

        private FakeUnauthorizedKeycloakServer(WebApplication app, string baseUrl)
        {
            _app = app;
            BaseUrl = baseUrl;
        }

        public string BaseUrl { get; }

        public static async Task<FakeUnauthorizedKeycloakServer> StartAsync()
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            var app = builder.Build();

            app.MapPost("/realms/{realm}/protocol/openid-connect/token", () =>
                Results.Json(new { error = "invalid_grant" }, statusCode: StatusCodes.Status401Unauthorized));

            await app.StartAsync();
            var baseUrl = app.Urls.Single(url => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)).TrimEnd('/');
            return new FakeUnauthorizedKeycloakServer(app, baseUrl);
        }

        public async ValueTask DisposeAsync()
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    private sealed record LoginResponse(string AccessToken);
}
