using GalacticTrader.Gateway;
using Microsoft.Extensions.Configuration;

namespace GalacticTrader.Gateway.Tests;

public sealed class GatewayRuntimeOptionsTests
{
    [Fact]
    public void FromConfiguration_UsesDefaultsWhenValuesMissing()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();

        var options = GatewayRuntimeOptions.FromConfiguration(configuration);

        Assert.Equal("http://api:8080", options.ApiBaseUrl);
        Assert.Equal("http://keycloak:8080/realms/galactictrader", options.JwtAuthority);
        Assert.Equal("account", options.JwtAudience);
        Assert.False(options.RequireHttpsMetadata);
        Assert.Equal(300, options.PermitLimit);
        Assert.Equal(60, options.WindowSeconds);
    }

    [Fact]
    public void FromConfiguration_ClampsRateLimitValues()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:RateLimit:PermitLimit"] = "1",
                ["Gateway:RateLimit:WindowSeconds"] = "100000"
            })
            .Build();

        var options = GatewayRuntimeOptions.FromConfiguration(configuration);

        Assert.Equal(10, options.PermitLimit);
        Assert.Equal(3600, options.WindowSeconds);
    }
}
