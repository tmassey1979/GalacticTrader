namespace GalacticTrader.IntegrationTests;

using System.Net;
using System.Net.Http.Json;

public sealed class AuthIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthIntegrationTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    [Fact]
    public async Task RegisterLoginAndValidateFlow_Succeeds()
    {
        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "integration_user",
            email = "integration_user@gt.test",
            password = "WarpDrive123"
        });

        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "integration_user",
            password = "WarpDrive123"
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        var loginPayload = await loginResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginPayload);
        Assert.False(string.IsNullOrWhiteSpace(loginPayload!.AccessToken));

        var validateResponse = await _client.GetAsync($"/api/auth/validate?token={loginPayload.AccessToken}");
        Assert.Equal(HttpStatusCode.OK, validateResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateUsername_ReturnsConflict()
    {
        var payload = new
        {
            username = "dupe_user",
            email = "dupe_a@gt.test",
            password = "WarpDrive123"
        };

        var firstResponse = await _client.PostAsJsonAsync("/api/auth/register", payload);
        var secondResponse = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = payload.username,
            email = "dupe_b@gt.test",
            password = payload.password
        });

        Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondResponse.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidPayload_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "ab",
            email = "not-an-email",
            password = "123"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        await _client.PostAsJsonAsync("/api/auth/register", new
        {
            username = "auth_target",
            email = "auth_target@gt.test",
            password = "WarpDrive123"
        });

        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            username = "auth_target",
            password = "WrongPassword"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private sealed record LoginResponse(string AccessToken);
}
