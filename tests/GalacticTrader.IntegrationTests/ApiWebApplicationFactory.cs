namespace GalacticTrader.IntegrationTests;

using GalacticTrader.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll(typeof(DbContextOptions<GalacticTraderDbContext>));
            services.AddDbContext<GalacticTraderDbContext>(options =>
                options.UseInMemoryDatabase($"gt-integration-{Guid.NewGuid():N}"));
        });
    }
}
