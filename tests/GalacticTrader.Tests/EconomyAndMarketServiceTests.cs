using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using GalacticTrader.Services.Economy;
using GalacticTrader.Services.Market;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace GalacticTrader.Tests;

public sealed class EconomyAndMarketServiceTests
{
    [Fact]
    public async Task CalculatePriceAsync_ReturnsCalculatedPriceUsingMultipliers()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedMarketDataAsync(dbContext);
        var economy = new EconomyService(dbContext, NullLogger<EconomyService>.Instance);

        var result = await economy.CalculatePriceAsync(new PriceCalculationInput
        {
            MarketListingId = seeded.ListingId,
            DemandMultiplier = 1.2f,
            RiskPremium = 0.15f,
            ScarcityModifier = 1.3f,
            FactionStabilityModifier = 1.01f,
            PirateActivityModifier = 1.10f,
            MonopolyModifier = 1.05f
        });

        Assert.NotNull(result);
        Assert.True(result!.CalculatedPrice > result.BasePrice);
    }

    [Fact]
    public async Task ProcessMarketTickAsync_UpdatesListings_AndWritesPriceHistory()
    {
        await using var dbContext = CreateDbContext();
        await SeedMarketDataAsync(dbContext);
        var economy = new EconomyService(dbContext, NullLogger<EconomyService>.Instance);

        var tick = await economy.ProcessMarketTickAsync();

        Assert.True(tick.MarketsProcessed >= 1);
        Assert.True(tick.ListingsUpdated >= 1);
        Assert.True(await dbContext.MarketPriceHistories.AnyAsync());
    }

    [Fact]
    public async Task ExecuteTradeAsync_BuyAndReverse_UpdatesTransactionStatus()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedMarketDataAsync(dbContext);
        var marketService = new MarketTransactionService(dbContext, NullLogger<MarketTransactionService>.Instance);

        var buy = await marketService.ExecuteTradeAsync(new ExecuteTradeRequest
        {
            PlayerId = seeded.PlayerId,
            ShipId = seeded.ShipId,
            MarketListingId = seeded.ListingId,
            ActionType = TradeActionType.Buy,
            Quantity = 5
        });

        Assert.Equal("completed", buy.Status);

        var reversed = await marketService.ReverseTradeAsync(new ReverseTradeRequest
        {
            TradeTransactionId = buy.TradeTransactionId,
            Reason = "test reversal"
        });

        Assert.NotNull(reversed);
        Assert.Equal("reversed", reversed!.Status);
    }

    [Fact]
    public async Task ExecuteTradeAsync_Blocks_ExploitiveRate()
    {
        await using var dbContext = CreateDbContext();
        var seeded = await SeedMarketDataAsync(dbContext);
        var marketService = new MarketTransactionService(dbContext, NullLogger<MarketTransactionService>.Instance);

        // Seed many rapid transactions to trigger rate limiter.
        for (var i = 0; i < 26; i++)
        {
            dbContext.TradeTransactions.Add(new TradeTransaction
            {
                Id = Guid.NewGuid(),
                PlayerId = seeded.PlayerId,
                SellerId = Guid.Empty,
                CommodityId = seeded.CommodityId,
                FromMarketId = seeded.MarketId,
                ToMarketId = seeded.MarketId,
                Quantity = 1,
                PricePerUnit = 100m,
                TotalPrice = 100m,
                Tariff = 0,
                TaxAmount = 0,
                TransactionFee = 0,
                InsuranceCost = 0,
                NetProfit = -100m,
                Status = "completed",
                CreatedAt = DateTime.UtcNow,
                CompletedAt = DateTime.UtcNow
            });
        }

        await dbContext.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            marketService.ExecuteTradeAsync(new ExecuteTradeRequest
            {
                PlayerId = seeded.PlayerId,
                ShipId = seeded.ShipId,
                MarketListingId = seeded.ListingId,
                ActionType = TradeActionType.Buy,
                Quantity = 1
            }));
    }

    private static async Task<(Guid PlayerId, Guid ShipId, Guid MarketId, Guid ListingId, Guid CommodityId)> SeedMarketDataAsync(
        GalacticTraderDbContext dbContext)
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
            Username = "market-player",
            Email = "market-player@test.local",
            KeycloakUserId = Guid.NewGuid(),
            LiquidCredits = 1_000_000m,
            ProtectionStatus = "None"
        });

        dbContext.Sectors.Add(new Sector
        {
            Id = sectorId,
            Name = "TradeHub",
            X = 0,
            Y = 0,
            Z = 0,
            SecurityLevel = 70,
            HazardRating = 20,
            PiratePresenceProbability = 10
        });

        dbContext.Ships.Add(new Ship
        {
            Id = shipId,
            PlayerId = playerId,
            Name = "Trader-1",
            ShipClass = "Freighter",
            HullIntegrity = 500,
            MaxHullIntegrity = 500,
            ShieldCapacity = 100,
            MaxShieldCapacity = 100,
            ReactorOutput = 80,
            CargoCapacity = 500,
            CargoUsed = 0,
            CurrentSectorId = sectorId
        });

        dbContext.Commodities.Add(new Commodity
        {
            Id = commodityId,
            Name = "Refined Metals",
            Category = "industrial",
            Description = "Processed ore",
            Volume = 1.0f,
            BasePrice = 100f,
            LegalityFactor = 0.8f,
            Rarity = 20f
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
            BasePrice = 100m,
            CurrentPrice = 100m,
            DemandMultiplier = 1f,
            RiskPremium = 0.05f,
            ScarcityModifier = 1f,
            AvailableQuantity = 1_000,
            MaxQuantity = 2_000,
            MinQuantity = 100,
            PriceLastChanged = DateTime.UtcNow
        });

        await dbContext.SaveChangesAsync();
        return (playerId, shipId, marketId, listingId, commodityId);
    }

    private static GalacticTraderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<GalacticTraderDbContext>()
            .UseInMemoryDatabase($"economy-market-tests-{Guid.NewGuid():N}")
            .Options;
        return new GalacticTraderDbContext(options);
    }
}
