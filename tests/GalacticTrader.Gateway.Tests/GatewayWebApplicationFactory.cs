using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace GalacticTrader.Gateway.Tests;

internal sealed class GatewayWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly int _permitLimit;
    private readonly int _windowSeconds;

    public GatewayWebApplicationFactory(int permitLimit = 10, int windowSeconds = 60)
    {
        _permitLimit = permitLimit;
        _windowSeconds = windowSeconds;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configBuilder) =>
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:ApiBaseUrl"] = "http://127.0.0.1:65530",
                ["Gateway:Jwt:Authority"] = "http://localhost/mock-realm",
                ["Gateway:Jwt:Audience"] = "account",
                ["Gateway:Jwt:RequireHttpsMetadata"] = "false",
                ["Gateway:RateLimit:PermitLimit"] = _permitLimit.ToString(),
                ["Gateway:RateLimit:WindowSeconds"] = _windowSeconds.ToString()
            });
        });
    }
}
