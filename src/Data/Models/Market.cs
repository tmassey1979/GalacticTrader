using System;
using System.Collections.Generic;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a market in a sector
    /// </summary>
    public class Market
    {
        public Guid Id { get; set; }
        public Guid SectorId { get; set; }
        public DateTime LastUpdated { get; set; }
        
        // Navigation
        public Sector Sector { get; set; }
        public ICollection<MarketListing> Listings { get; set; } = new List<MarketListing>();
    }

    /// <summary>
    /// Individual commodity listing in a market
    /// </summary>
    public class MarketListing
    {
        public Guid Id { get; set; }
        public Guid MarketId { get; set; }
        public Guid CommodityId { get; set; }
        
        // Pricing
        public decimal BasePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public float DemandMultiplier { get; set; }
        public float RiskPremium { get; set; }
        public float ScarcityModifier { get; set; }
        
        // Supply
        public long AvailableQuantity { get; set; }
        public long MaxQuantity { get; set; }
        public long MinQuantity { get; set; }
        
        // Economic Data
        public long TotalTradeVolume24h { get; set; }
        public float PriceChangePercent24h { get; set; }
        public DateTime PriceLastChanged { get; set; }
        
        // Navigation
        public Market Market { get; set; }
        public Commodity Commodity { get; set; }
        public ICollection<MarketPriceHistory> PriceHistory { get; set; } = new List<MarketPriceHistory>();
    }

    /// <summary>
    /// Commodity type
    /// </summary>
    public class Commodity
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Description { get; set; }
        
        // Properties
        public float Volume { get; set; } // Volume per unit
        public float BasePrice { get; set; }
        public float LegalityFactor { get; set; } // -1 (illegal) to 1 (legal)
        public float Rarity { get; set; } // 0-100
        
        // Navigation
        public ICollection<MarketListing> MarketListings { get; set; } = new List<MarketListing>();
        public ICollection<Cargo> Cargo { get; set; } = new List<Cargo>();
    }

    /// <summary>
    /// Price history for market tracking
    /// </summary>
    public class MarketPriceHistory
    {
        public Guid Id { get; set; }
        public Guid MarketListingId { get; set; }
        public DateTime RecordedAt { get; set; }
        
        public decimal Price { get; set; }
        public long Quantity { get; set; }
        public long VolumeTraded { get; set; }
        
        public MarketListing MarketListing { get; set; }
    }
}
