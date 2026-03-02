using System;
using System.Collections.Generic;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a faction in the universe
    /// </summary>
    public class Faction
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        
        // Alignment
        public int AlignmentBias { get; set; } // -100 (criminal) to 100 (lawful)
        public float InfluenceScore { get; set; }
        public float WealthScore { get; set; }
        public float PowerScore { get; set; }
        
        // Reputation Pool
        public double ReputationMultiplier { get; set; }
        public int ReputationDecayPerDay { get; set; }
        
        // Control & Territory
        public int ControlledSectors { get; set; }
        public decimal TreasuryBalance { get; set; }
        
        // Trade
        public decimal TradeGoodModifier { get; set; }
        public decimal TaxRate { get; set; }
        
        // Navigation
        public ICollection<PlayerFactionRelationship> PlayerRelationships { get; set; } = new List<PlayerFactionRelationship>();
        public ICollection<Sector> ControlledSectors_Nav { get; set; } = new List<Sector>();
        public ICollection<NPCAgent> Members { get; set; } = new List<NPCAgent>();
    }
}
