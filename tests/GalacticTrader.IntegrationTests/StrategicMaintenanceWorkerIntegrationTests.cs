namespace GalacticTrader.IntegrationTests;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

public sealed class StrategicMaintenanceWorkerIntegrationTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public StrategicMaintenanceWorkerIntegrationTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task IntelligenceExpiryWorker_ExpiresStaleReportsOnSchedule()
    {
        var configuredFactory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Strategic:IntelligenceExpiryWorker:Enabled"] = "true",
                    ["Strategic:IntelligenceExpiryWorker:IntervalSeconds"] = "1"
                });
            });
        });

        using var client = configuredFactory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });

        var reportId = Guid.NewGuid();
        await SeedExpiredReportAsync(configuredFactory.Services, reportId);

        var expired = await WaitForConditionAsync(
            async () => await IsReportExpiredAsync(configuredFactory.Services, reportId),
            timeout: TimeSpan.FromSeconds(8),
            pollingInterval: TimeSpan.FromMilliseconds(200));

        Assert.True(expired);
    }

    private static async Task SeedExpiredReportAsync(IServiceProvider services, Guid reportId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();

        var player = new Player
        {
            Id = Guid.NewGuid(),
            Username = $"worker-player-{Guid.NewGuid():N}"[..24],
            Email = $"worker-{Guid.NewGuid():N}@gt.test",
            KeycloakUserId = Guid.NewGuid(),
            NetWorth = 10_000m,
            LiquidCredits = 10_000m,
            ReputationScore = 0,
            AlignmentLevel = 0,
            FleetStrengthRating = 1,
            ProtectionStatus = "Protected",
            CreatedAt = DateTime.UtcNow.AddDays(-3),
            LastActiveAt = DateTime.UtcNow,
            IsActive = true
        };

        var sector = new Sector
        {
            Id = Guid.NewGuid(),
            Name = $"worker-sector-{Guid.NewGuid():N}"[..24],
            X = 0f,
            Y = 0f,
            Z = 0f,
            SecurityLevel = 50,
            HazardRating = 20,
            ResourceModifier = 1f,
            EconomicIndex = 50,
            SensorInterferenceLevel = 0f,
            ControlledByFactionId = null,
            AverageTrafficLevel = 20,
            PiratePresenceProbability = 10
        };

        var network = new IntelligenceNetwork
        {
            Id = Guid.NewGuid(),
            OwnerPlayerId = player.Id,
            Name = $"worker-network-{Guid.NewGuid():N}"[..20],
            AssetCount = 5,
            CoverageScore = 40f,
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddHours(-6),
            UpdatedAt = DateTime.UtcNow.AddHours(-1)
        };

        var report = new IntelligenceReport
        {
            Id = reportId,
            NetworkId = network.Id,
            SectorId = sector.Id,
            SignalType = "traffic",
            ConfidenceScore = 65f,
            Payload = "stale report",
            DetectedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddMinutes(-30),
            IsExpired = false
        };

        dbContext.Players.Add(player);
        dbContext.Sectors.Add(sector);
        dbContext.IntelligenceNetworks.Add(network);
        dbContext.IntelligenceReports.Add(report);
        await dbContext.SaveChangesAsync();
    }

    private static async Task<bool> IsReportExpiredAsync(IServiceProvider services, Guid reportId)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<GalacticTraderDbContext>();
        return await dbContext.IntelligenceReports
            .AsNoTracking()
            .Where(report => report.Id == reportId)
            .Select(report => report.IsExpired)
            .SingleAsync();
    }

    private static async Task<bool> WaitForConditionAsync(
        Func<Task<bool>> condition,
        TimeSpan timeout,
        TimeSpan pollingInterval)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow <= deadline)
        {
            if (await condition())
            {
                return true;
            }

            await Task.Delay(pollingInterval);
        }

        return false;
    }
}
