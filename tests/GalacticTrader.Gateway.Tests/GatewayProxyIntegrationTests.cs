using System.Net;

namespace GalacticTrader.Gateway.Tests;

public sealed class GatewayProxyIntegrationTests : IAsyncLifetime
{
    private GatewayWebApplicationFactory? _gatewayFactory;
    private HttpClient? _gatewayClient;

    public Task InitializeAsync()
    {
        _gatewayFactory = new GatewayWebApplicationFactory(permitLimit: 10, windowSeconds: 60);
        _gatewayClient = _gatewayFactory.CreateClient();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        if (_gatewayClient is not null)
        {
            _gatewayClient.Dispose();
        }

        if (_gatewayFactory is not null)
        {
            await _gatewayFactory.DisposeAsync();
        }
    }

    [Fact]
    public async Task OpenAuthRoute_DoesNotRequireJwt()
    {
        var response = await _gatewayClient!.GetAsync("/api/auth/probe");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedApiRoute_RequiresJwt()
    {
        var response = await _gatewayClient!.GetAsync("/api/secure/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

}
