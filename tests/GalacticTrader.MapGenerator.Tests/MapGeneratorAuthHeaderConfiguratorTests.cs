using GalacticTrader.MapGenerator.Api;
using System.Net.Http;

namespace GalacticTrader.MapGenerator.Tests;

public sealed class MapGeneratorAuthHeaderConfiguratorTests
{
    [Fact]
    public void ApplyBearerToken_SetsAuthorization_WhenTokenProvided()
    {
        using var httpClient = new HttpClient();

        MapGeneratorAuthHeaderConfigurator.ApplyBearerToken(httpClient, "  abc123  ");

        Assert.NotNull(httpClient.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", httpClient.DefaultRequestHeaders.Authorization!.Scheme);
        Assert.Equal("abc123", httpClient.DefaultRequestHeaders.Authorization!.Parameter);
    }

    [Fact]
    public void ApplyBearerToken_ClearsAuthorization_WhenTokenBlank()
    {
        using var httpClient = new HttpClient();
        MapGeneratorAuthHeaderConfigurator.ApplyBearerToken(httpClient, "seed");

        MapGeneratorAuthHeaderConfigurator.ApplyBearerToken(httpClient, " ");

        Assert.Null(httpClient.DefaultRequestHeaders.Authorization);
    }
}
