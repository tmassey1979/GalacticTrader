namespace GalacticTrader.Services.Economy;

using GalacticTrader.Data;
using GalacticTrader.Data.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

public sealed class EconomyService : IEconomyService
{
    private static readonly Lock ShockLock = new();
    private static readonly Dictionary<Guid, ShockState> MarketShocks = [];

    private readonly GalacticTraderDbContext _dbContext;
    private readonly ILogger<EconomyService> _logger;

    public EconomyService(GalacticTraderDbContext dbContext, ILogger<EconomyService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<PriceCalculationResult?> CalculatePriceAsync(
        PriceCalculationInput input,
        CancellationToken cancellationToken = default)
    {
        var listing = await _dbContext.MarketListings
            .AsNoTracking()
            .Include(marketListing => marketListing.Market)
            .FirstOrDefaultAsync(marketListing => marketListing.Id == input.MarketListingId, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        var calculated = CalculatePrice(
            listing.BasePrice,
            input.DemandMultiplier,
            input.RiskPremium,
            input.ScarcityModifier,
            input.FactionStabilityModifier,
            input.PirateActivityModifier,
            input.MonopolyModifier,
            GetShockMultiplier(listing.MarketId));

        return new PriceCalculationResult
        {
            MarketListingId = listing.Id,
            BasePrice = listing.BasePrice,
            CurrentPrice = listing.CurrentPrice,
            CalculatedPrice = calculated,
            DemandMultiplier = input.DemandMultiplier,
            RiskPremium = input.RiskPremium,
            ScarcityModifier = input.ScarcityModifier,
            MarketEfficiency = CalculateMarketEfficiency(listing)
        };
    }

    public async Task<MarketTickResult> ProcessMarketTickAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var markets = await _dbContext.Markets
            .Include(market => market.Sector)
            .Include(market => market.Listings)
            .ThenInclude(listing => listing.Commodity)
            .ToListAsync(cancellationToken);

        var listingsUpdated = 0;
        var shocksTriggered = 0;

        foreach (var market in markets)
        {
            var shockMultiplier = GetShockMultiplier(market.Id);
            foreach (var listing in market.Listings)
            {
                // Supply and demand recalculation.
                var demandMultiplier = RecalculateDemandMultiplier(listing);
                var scarcityModifier = RecalculateScarcityModifier(listing);
                var riskPremium = RecalculateRiskPremium(listing, market);
                var factionStability = GetFactionStabilityModifier(market.Sector?.ControlledByFactionId);
                var pirateActivity = GetPirateActivityModifier(market.Sector?.PiratePresenceProbability ?? 0);
                var monopolyModifier = GetMonopolyModifier(listing, market);

                // NPC trade volume generation and market clearance logic.
                var npcTradeVolume = GenerateNpcTradeVolume(listing);
                listing.TotalTradeVolume24h = Math.Max(0, listing.TotalTradeVolume24h + npcTradeVolume);
                listing.AvailableQuantity = ApplyMarketClearance(listing.AvailableQuantity, npcTradeVolume, listing.MaxQuantity);

                var newPrice = CalculatePrice(
                    listing.BasePrice,
                    demandMultiplier,
                    riskPremium,
                    scarcityModifier,
                    factionStability,
                    pirateActivity,
                    monopolyModifier,
                    shockMultiplier);

                // Price stabilization: cap per-tick volatility to 15%.
                var stabilizedPrice = StabilizePrice(listing.CurrentPrice, newPrice, 0.15m);
                if (listing.CurrentPrice > 0)
                {
                    listing.PriceChangePercent24h = (float)(((stabilizedPrice - listing.CurrentPrice) / listing.CurrentPrice) * 100m);
                }

                listing.DemandMultiplier = demandMultiplier;
                listing.ScarcityModifier = scarcityModifier;
                listing.RiskPremium = riskPremium;
                listing.CurrentPrice = stabilizedPrice;
                listing.PriceLastChanged = now;

                _dbContext.MarketPriceHistories.Add(new MarketPriceHistory
                {
                    Id = Guid.NewGuid(),
                    MarketListingId = listing.Id,
                    RecordedAt = now,
                    Price = stabilizedPrice,
                    Quantity = listing.AvailableQuantity,
                    VolumeTraded = npcTradeVolume
                });

                listingsUpdated++;
            }

            market.LastUpdated = now;
            if (Random.Shared.NextDouble() < 0.03)
            {
                await TriggerMarketShockAsync(new MarketShockRequest
                {
                    MarketId = market.Id,
                    Intensity = (float)Math.Round(Random.Shared.NextDouble() * 0.3 + 0.1, 2),
                    Reason = "VolatilitySpike"
                }, cancellationToken);
                shocksTriggered++;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        CleanupExpiredShocks();

        return new MarketTickResult
        {
            ProcessedAtUtc = now,
            MarketsProcessed = markets.Count,
            ListingsUpdated = listingsUpdated,
            ShockEventsTriggered = shocksTriggered
        };
    }

    public Task<bool> TriggerMarketShockAsync(MarketShockRequest request, CancellationToken cancellationToken = default)
    {
        if (request.MarketId == Guid.Empty)
        {
            return Task.FromResult(false);
        }

        var intensity = Math.Clamp(request.Intensity, 0f, 1f);
        lock (ShockLock)
        {
            MarketShocks[request.MarketId] = new ShockState
            {
                MarketId = request.MarketId,
                Multiplier = 1f + intensity,
                Reason = request.Reason,
                ExpiresAtUtc = DateTime.UtcNow.AddMinutes(10)
            };
        }

        _logger.LogInformation(
            "Market shock triggered. MarketId={MarketId}, Intensity={Intensity}, Reason={Reason}",
            request.MarketId,
            intensity,
            request.Reason);
        return Task.FromResult(true);
    }

    public async Task<IReadOnlyList<CommodityHierarchyItem>> GetCommodityHierarchyAsync(CancellationToken cancellationToken = default)
    {
        var commodities = await _dbContext.Commodities
            .AsNoTracking()
            .OrderBy(commodity => commodity.Category)
            .ThenBy(commodity => commodity.Name)
            .ToListAsync(cancellationToken);

        return commodities.Select(commodity => new CommodityHierarchyItem
        {
            CommodityId = commodity.Id,
            Name = commodity.Name,
            Category = commodity.Category,
            HierarchyTier = ResolveCommodityTier(commodity),
            LegalityFactor = commodity.LegalityFactor,
            Rarity = commodity.Rarity
        }).ToList();
    }

    private static decimal CalculatePrice(
        decimal basePrice,
        float demandMultiplier,
        float riskPremium,
        float scarcityModifier,
        float factionStabilityModifier,
        float pirateActivityModifier,
        float monopolyModifier,
        float shockMultiplier)
    {
        var multiplier =
            demandMultiplier *
            (1f + riskPremium) *
            scarcityModifier *
            factionStabilityModifier *
            pirateActivityModifier *
            monopolyModifier *
            shockMultiplier;

        var clampedMultiplier = Math.Clamp(multiplier, 0.10f, 10.0f);
        return Math.Max(1m, decimal.Round(basePrice * (decimal)clampedMultiplier, 2));
    }

    private static float RecalculateDemandMultiplier(MarketListing listing)
    {
        if (listing.MaxQuantity <= 0)
        {
            return 1f;
        }

        var utilization = 1f - Math.Clamp((float)listing.AvailableQuantity / listing.MaxQuantity, 0f, 1f);
        return Math.Clamp(1f + (utilization * 0.8f), 0.6f, 2.0f);
    }

    private static float RecalculateScarcityModifier(MarketListing listing)
    {
        if (listing.AvailableQuantity <= listing.MinQuantity)
        {
            return 1.5f;
        }

        if (listing.AvailableQuantity >= listing.MaxQuantity)
        {
            return 0.85f;
        }

        return Math.Clamp(1f + ((listing.MaxQuantity - listing.AvailableQuantity) / (float)Math.Max(1, listing.MaxQuantity)), 0.8f, 1.6f);
    }

    private static float RecalculateRiskPremium(MarketListing listing, Market market)
    {
        var legalityPremium = listing.Commodity?.LegalityFactor < 0 ? 0.15f : 0.03f;
        var sectorRisk = (market.Sector?.HazardRating ?? 0) / 500f;
        return Math.Clamp(legalityPremium + sectorRisk, 0f, 0.8f);
    }

    private static float GetFactionStabilityModifier(Guid? factionId)
    {
        if (!factionId.HasValue)
        {
            return 1f;
        }

        return 1.02f;
    }

    private static float GetPirateActivityModifier(int piratePresenceProbability)
    {
        return Math.Clamp(1f + (piratePresenceProbability / 200f), 1f, 1.5f);
    }

    private static float GetMonopolyModifier(MarketListing listing, Market market)
    {
        var saturation = listing.AvailableQuantity / (float)Math.Max(1, listing.MaxQuantity);
        return Math.Clamp(1f + ((0.5f - saturation) * 0.4f), 0.8f, 1.3f);
    }

    private static long GenerateNpcTradeVolume(MarketListing listing)
    {
        var baseline = (long)Math.Max(1, Math.Round(listing.MaxQuantity * 0.02));
        var variance = Random.Shared.NextInt64(0, baseline + 1);
        return baseline + variance;
    }

    private static long ApplyMarketClearance(long availableQuantity, long npcTradeVolume, long maxQuantity)
    {
        var postTrade = Math.Max(0, availableQuantity - npcTradeVolume);
        if (postTrade == 0)
        {
            return (long)Math.Round(maxQuantity * 0.4);
        }

        return Math.Min(maxQuantity, postTrade + (long)Math.Round(maxQuantity * 0.01));
    }

    private static decimal StabilizePrice(decimal currentPrice, decimal targetPrice, decimal maxTickChangePercent)
    {
        if (currentPrice <= 0)
        {
            return targetPrice;
        }

        var maxDelta = currentPrice * maxTickChangePercent;
        var delta = targetPrice - currentPrice;
        var clampedDelta = Math.Clamp(delta, -maxDelta, maxDelta);
        return decimal.Round(currentPrice + clampedDelta, 2);
    }

    private static float CalculateMarketEfficiency(MarketListing listing)
    {
        var volatilityPenalty = Math.Min(Math.Abs(listing.PriceChangePercent24h) / 100f, 0.5f);
        var stockHealth = Math.Clamp(listing.AvailableQuantity / (float)Math.Max(1, listing.MaxQuantity), 0f, 1f);
        return Math.Clamp((1f - volatilityPenalty) * 0.6f + stockHealth * 0.4f, 0f, 1f);
    }

    private static string ResolveCommodityTier(Commodity commodity)
    {
        if (commodity.LegalityFactor < 0)
        {
            return "Contraband";
        }

        return commodity.Category.ToLowerInvariant() switch
        {
            "ore" or "raw" => "Tier-1 Raw",
            "industrial" => "Tier-2 Industrial",
            "technology" => "Tier-3 Advanced",
            "luxury" => "Tier-4 Luxury",
            _ => "Tier-2 Standard"
        };
    }

    private static float GetShockMultiplier(Guid marketId)
    {
        lock (ShockLock)
        {
            if (!MarketShocks.TryGetValue(marketId, out var shock))
            {
                return 1f;
            }

            if (shock.ExpiresAtUtc < DateTime.UtcNow)
            {
                MarketShocks.Remove(marketId);
                return 1f;
            }

            return shock.Multiplier;
        }
    }

    private static void CleanupExpiredShocks()
    {
        lock (ShockLock)
        {
            var expired = MarketShocks.Values
                .Where(shock => shock.ExpiresAtUtc < DateTime.UtcNow)
                .Select(shock => shock.MarketId)
                .ToList();
            foreach (var marketId in expired)
            {
                MarketShocks.Remove(marketId);
            }
        }
    }
}
