using System;
using System.Collections.Generic;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents an NPC autonomous agent
    /// </summary>
    public class NPCAgent
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Archetype { get; set; } = string.Empty; // "Merchant", "Pirate", "Industrialist", etc.
        public Guid? FactionId { get; set; }
        
        // Personality & Behavior
        public float WealthTarget { get; set; }
        public float RiskTolerance { get; set; } // 0-100
        public int AggressionIndex { get; set; } // -100 to 100
        
        // State
        public decimal Wealth { get; set; }
        public int ReputationScore { get; set; }
        public float InfluenceScore { get; set; }
        public int FleetSize { get; set; }
        
        // Current Activity
        public Guid? CurrentLocationId { get; set; }
        public Guid? TargetLocationId { get; set; }
        public string CurrentGoal { get; set; } = string.Empty;
        public int DecisionTick { get; set; }
        
        // Trading
        public decimal TradeVolume24h { get; set; }
        public bool TradesLegally { get; set; }
        public bool TradesIllegally { get; set; }
        
        // Navigation
        public Faction? Faction { get; set; }
        public Sector? CurrentLocation { get; set; }
        public Sector? TargetLocation { get; set; }
        public ICollection<NPCShip> Ships { get; set; } = new List<NPCShip>();
    }

    /// <summary>
    /// Represents an NPC's ship
    /// </summary>
    public class NPCShip
    {
        public Guid Id { get; set; }
        public Guid NPCAgentId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ShipClass { get; set; } = string.Empty;
        
        public int HullIntegrity { get; set; }
        public int MaxHullIntegrity { get; set; }
        public int CombatRating { get; set; }
        public Guid? CurrentSectorId { get; set; }
        public bool IsActive { get; set; }
        
        public NPCAgent Agent { get; set; } = null!;
        public Sector? CurrentSector { get; set; }
    }
}
