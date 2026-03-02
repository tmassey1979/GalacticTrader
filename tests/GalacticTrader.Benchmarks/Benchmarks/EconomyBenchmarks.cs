namespace GalacticTrader.Benchmarks;

using BenchmarkDotNet.Attributes;
using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Economy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

[MemoryDiagnoser]
public class EconomyBenchmarks
{
    private GalacticTraderDbContext _dbContext = null!;
    private EconomyService _economy = null!;
    private Guid _listingId;

    [GlobalSetup]
    public async Task SetupAsync()
    {
        _dbContext = CreateDbContext();
        _listingId = await SeedMarketDataAsync(_dbContext);
        _economy = new EconomyService(_dbContext, NullLogger<EconomyService>.Instance);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _dbContext.Dispose();
    }

    [Benchmark]
    public Task<MarketTickResult> ProcessMarketTick()
    {
        return _economy.ProcessMarketTickAsync();
    }

    [Benchmark]
    public Task<PriceCalculationResult?> CalculatePricePreview()
    {
        return _economy.CalculatePriceAsync(new PriceCalculationInput
        {
            MarketListingId = _listingId,
            DemandMultiplier = 1.15f,
            RiskPremium = 0.18f,
            ScarcityModifier = 1.22f,
            FactionStabilityModifier = 1.01f,
            PirateActivityModifier = 1.06f,
            MonopolyModifier = 1.03f
        });
    }

    private static async Task<Guid> SeedMarketDataAsync(GalacticTraderDbContext dbContext)
    {
        var playerId = Guid.NewGuid();
        var shipId = Guid.NewGuid();
        var commodityId = Guid.NewGuid();
        var sectorId = Guid.NewGuid();
        var marketId = Guid.NewGuid();
        var listingId = Guid.NewGuid();

        dbContext.Players.Add(new Player
        {
            Id = playerId,
            Username = "benchmark-player",
            Email = "benchmark-player@gt.test",
            KeycloakUserId = Guid.NewGuid(),
            LiquidCredits = 5_000_000m,
            ProtectionStatus = "None"
        });

        dbContext.Sectors.Add(new Sector
        {
            Id = sectorId,
            Name = "Benchmark Hub",
            X = 0,
            Y = 0,
            Z = 0,
            SecurityLevel = 75,
            HazardRating = 15,
            PiratePresenceProbability = 8
        });

        dbContext.Ships.Add(new Ship
        {
            Id = shipId,
            PlayerId = playerId,
            Name = "Benchmark Freighter",
            ShipClass = "Freighter",
            HullIntegrity = 450,
            MaxHullIntegrity = 450,
            ShieldCapacity = 120,
            MaxShieldCapacity = 120,
            ReactorOutput = 90,
            CargoCapacity = 700,
            CurrentSectorId = sectorId
        });

        dbContext.Commodities.Add(new Commodity
        {
            Id = commodityId,
            Name = "Nano Electronics",
            Category = "technology",
            Description = "High density trade good",
            Volume = 1.0f,
            BasePrice = 240f,
            LegalityFactor = 0.95f,
            Rarity = 35f
        });

        dbContext.Markets.Add(new Market
        {
            Id = marketId,
            SectorId = sectorId,
            LastUpdated = DateTime.UtcNow
        });

        dbContext.MarketListings.Add(new MarketListing
        {
            Id = listingId,
            MarketId = marketId,
            CommodityId = commodityId,
            BasePrice = 240m,
            CurrentPrice = 240m,
            DemandMultiplier = 1f,
            RiskPremium = 0.04f,
            ScarcityModifier = 1f,
            AvailableQuantity = 1_400,
            MaxQuantity = 2_600,
            MinQuantity = 120,
            PriceLastChanged = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return listingId;
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"economy-bench-{Guid.NewGuid():N}")
            .Options;

        return new GalacticTraderDbContext(options);
    }
}
