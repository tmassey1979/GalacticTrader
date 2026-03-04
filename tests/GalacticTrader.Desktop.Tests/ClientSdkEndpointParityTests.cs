using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Realtime;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;

namespace GalacticTrader.Desktop.Tests;

public sealed class ClientSdkEndpointParityTests
{
    [Fact]
    public async Task AuthLogin_UsesLegacyLoginEndpoint()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new AuthLoginResultDto
            {
                AccessToken = "token-123",
                Player = new AuthPlayerIdentityDto
                {
                    PlayerId = playerId,
                    Username = "viper",
                    Email = "viper@example.com"
                }
            })
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new AuthApiClient(httpClient);

        var session = await client.LoginAsync("viper", "secret");

        Assert.Equal(playerId, session.PlayerId);
        Assert.Equal("/api/auth/login", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Post, handler.LastRequest.Method);
    }

    [Fact]
    public async Task NavigationGetSectors_UsesLegacyEndpoint()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<SectorApiDto>())
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new NavigationApiClient(httpClient);
        await client.GetSectorsAsync();

        Assert.Equal("/api/navigation/sectors", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal(HttpMethod.Get, handler.LastRequest.Method);
    }

    [Fact]
    public async Task MarketTransactions_UsesLegacyEndpoint()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<TradeExecutionResultApiDto>())
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new MarketApiClient(httpClient);
        await client.GetTransactionsAsync(playerId);

        Assert.Equal($"/api/market/transactions/{playerId:D}", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.StartsWith("limit=", handler.LastRequest.RequestUri.Query.TrimStart('?'));
    }

    [Fact]
    public async Task FleetShips_UsesLegacyEndpoint()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<ShipApiDto>())
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new FleetApiClient(httpClient);
        await client.GetPlayerShipsAsync(playerId);

        Assert.Equal($"/api/fleet/players/{playerId:D}/ships", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task ReputationStandings_UsesLegacyEndpoint()
    {
        var playerId = Guid.NewGuid();
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(Array.Empty<PlayerFactionStandingApiDto>())
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new ReputationApiClient(httpClient);
        await client.GetFactionStandingsAsync(playerId);

        Assert.Equal($"/api/reputation/factions/{playerId:D}", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task StrategicErrors_ThrowStandardizedApiClientException()
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("table missing")
        });

        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost:8080") };
        var client = new StrategicApiClient(httpClient);

        var exception = await Assert.ThrowsAsync<ApiClientException>(() => client.GetTerritoryDominanceAsync());
        Assert.Equal("Load territory dominance failed", exception.Operation);
        Assert.Equal(HttpStatusCode.InternalServerError, exception.StatusCode);
        Assert.Contains("table missing", exception.Detail);
    }

    [Fact]
    public void StrategicRealtimeStream_BuildsLegacyWsEndpoint()
    {
        var playerId = Guid.NewGuid();
        var streamClient = new StrategicRealtimeStreamClient("http://localhost:8080", "token");
        var uri = InvokePrivateBuildUri(
            streamClient,
            [playerId, 7]);

        Assert.Equal("ws", uri.Scheme);
        Assert.Equal($"/api/strategic/ws/dashboard/{playerId:D}", uri.AbsolutePath);
        Assert.Equal("intervalSeconds=7", uri.Query.TrimStart('?'));
    }

    [Fact]
    public void CommunicationRealtimeStream_BuildsLegacyWsEndpoint()
    {
        var playerId = Guid.NewGuid();
        var streamClient = new CommunicationRealtimeStreamClient("https://example.com", "token");
        var uri = InvokePrivateBuildUri(
            streamClient,
            [playerId, "global", "desktop-feed"]);

        Assert.Equal("wss", uri.Scheme);
        Assert.Equal("/api/communication/ws/global/desktop-feed", uri.AbsolutePath);
        Assert.Equal($"playerId={playerId:D}", uri.Query.TrimStart('?'));
    }

    private static Uri InvokePrivateBuildUri(object instance, object[] parameters)
    {
        var method = instance
            .GetType()
            .GetMethod("BuildUri", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("BuildUri method is required for endpoint parity tests.");

        return (Uri)(method.Invoke(instance, parameters)
            ?? throw new InvalidOperationException("BuildUri invocation returned null."));
    }

    private sealed class RecordingHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder = responder;

        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }
}
