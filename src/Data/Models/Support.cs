using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Leaderboard rankings
    /// </summary>
    public class Leaderboard
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public string LeaderboardType { get; set; } = string.Empty; // "wealth", "reputation", "combat", "trade"
        
        public long Rank { get; set; }
        public decimal Score { get; set; }
        public decimal PreviousScore { get; set; }
        
        public DateTime LastUpdated { get; set; }
        
        public Player Player { get; set; } = null!;
    }

    /// <summary>
    /// Player's reputation with a faction
    /// </summary>
    public class PlayerFactionRelationship
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public Guid FactionId { get; set; }
        
        public int ReputationScore { get; set; } // -100 to 100
        public bool HasAccess { get; set; }
        public decimal TradingDiscount { get; set; }
        
        public DateTime UpdatedAt { get; set; }
        
        public Player Player { get; set; } = null!;
        public Faction Faction { get; set; } = null!;
    }

    /// <summary>
    /// Cargo in a ship
    /// </summary>
    public class Cargo
    {
        public Guid Id { get; set; }
        public Guid ShipId { get; set; }
        public Guid CommodityId { get; set; }
        
        public long Quantity { get; set; }
        public decimal ValuePerUnit { get; set; }
        public DateTime LoadedAt { get; set; }
        
        public Ship Ship { get; set; } = null!;
        public Commodity Commodity { get; set; } = null!;
    }

    /// <summary>
    /// Ship equipment modules
    /// </summary>
    public class ShipModule
    {
        public Guid Id { get; set; }
        public Guid ShipId { get; set; }
        public string ModuleType { get; set; } = string.Empty; // "weapon", "engine", "shield", "sensor", etc.
        public string Name { get; set; } = string.Empty;
        public int Tier { get; set; } // Equipment tier level
        
        public int HealthPoints { get; set; }
        public int MaxHealthPoints { get; set; }
        
        public decimal PurchasePrice { get; set; }
        public DateTime InstalledAt { get; set; }
        
        public Ship Ship { get; set; } = null!;
    }
}
