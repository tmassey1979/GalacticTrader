using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a combat battle log
    /// </summary>
    public class CombatLog
    {
        public Guid Id { get; set; }
        public Guid AttackerId { get; set; }
        public Guid? DefenderId { get; set; } // Can be null for NPC battles
        public Guid LocationSectorId { get; set; }
        
        // Combatants
        public Guid AttackerShipId { get; set; }
        public Guid? DefenderShipId { get; set; }
        
        // Battle Statistics
        public int AttackerInitialRating { get; set; }
        public int DefenderInitialRating { get; set; }
        public string BattleOutcome { get; set; } // "victory", "defeat", "draw"
        
        // Damage Report
        public int AttackerDamageDealt { get; set; }
        public int DefenderDamageDealt { get; set; }
        public int AttackerHullDamage { get; set; }
        public int DefenderHullDamage { get; set; }
        
        // Financial Impact
        public decimal AttackerReward { get; set; }
        public decimal DefenderCompensation { get; set; }
        public decimal InsurancePayout { get; set; }
        
        // Reputation Impact
        public int AttackerReputationChange { get; set; }
        public int DefenderReputationChange { get; set; }
        
        // Timestamps & Duration
        public DateTime BattleStartedAt { get; set; }
        public DateTime BattleEndedAt { get; set; }
        public int DurationSeconds { get; set; }
        public int TotalTicks { get; set; }
        
        // Navigation
        public Player Attacker { get; set; }
        public Player Defender { get; set; }
        public Sector Location { get; set; }
        public Ship AttackerShip { get; set; }
        public Ship DefenderShip { get; set; }
    }
}
