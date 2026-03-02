using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a player in the game
    /// </summary>
    public class Player
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public Guid KeycloakUserId { get; set; }
        
        // Financial Stats
        public decimal NetWorth { get; set; }
        public decimal LiquidCredits { get; set; }
        
        // Reputation & Alignment
        public int ReputationScore { get; set; }
        public int AlignmentLevel { get; set; } // -100 to 100: lawful to dirty
        
        // Fleet & Assets
        public int FleetStrengthRating { get; set; }
        public string ProtectionStatus { get; set; }
        
        // Game Stats
        public DateTime CreatedAt { get; set; }
        public DateTime LastActiveAt { get; set; }
        public bool IsActive { get; set; }
        
        // Navigation
        public ICollection<Ship> Ships { get; set; } = new List<Ship>();
        public ICollection<PlayerFactionRelationship> FactionRelationships { get; set; } = new List<PlayerFactionRelationship>();
        public ICollection<Crew> Crew { get; set; } = new List<Crew>();
        public ICollection<TradeTransaction> TradeTransactions { get; set; } = new List<TradeTransaction>();
        public ICollection<CombatLog> CombatLogs { get; set; } = new List<CombatLog>();
    }
}
