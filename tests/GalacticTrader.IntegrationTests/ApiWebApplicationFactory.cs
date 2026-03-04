namespace GalacticTrader.IntegrationTests;

using GalacticTrader.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"gt-integration-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configurationBuilder) =>
        {
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Keycloak:ServerUrl"] = string.Empty,
                ["Keycloak:Realm"] = string.Empty,
                ["Keycloak:ClientId"] = string.Empty
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<GalacticTraderDbContext>));
            services.AddDbContext<GalacticTraderDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
