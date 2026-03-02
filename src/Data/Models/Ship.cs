using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a spaceship in the game
    /// </summary>
    public class Ship
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public string Name { get; set; }
        public string ShipClass { get; set; }
        
        // Core Stats
        public int HullIntegrity { get; set; }
        public int MaxHullIntegrity { get; set; }
        public int ShieldCapacity { get; set; }
        public int MaxShieldCapacity { get; set; }
        public int ReactorOutput { get; set; }
        public int CargoCapacity { get; set; }
        public int CargoUsed { get; set; }
        public int SensorRange { get; set; }
        public int SignatureProfile { get; set; }
        
        // Slots & Hardpoints
        public int CrewSlots { get; set; }
        public int Hardpoints { get; set; }
        
        // Insurance & Status
        public bool HasInsurance { get; set; }
        public decimal InsuranceRate { get; set; }
        public bool IsActive { get; set; }
        public bool IsInCombat { get; set; }
        
        // Location
        public Guid? CurrentSectorId { get; set; }
        public Guid? TargetSectorId { get; set; }
        public int StatusId { get; set; } // 0=Docked, 1=Traveling, 2=Drifting, 3=InCombat
        
        // Financial
        public decimal PurchasePrice { get; set; }
        public DateTime PurchasedAt { get; set; }
        public decimal CurrentValue { get; set; }
        
        // Navigation
        public Player Player { get; set; }
        public Sector CurrentSector { get; set; }
        public Sector TargetSector { get; set; }
        public ICollection<Crew> Crew { get; set; } = new List<Crew>();
        public ICollection<Cargo> Cargo { get; set; } = new List<Cargo>();
        public ICollection<ShipModule> Modules { get; set; } = new List<ShipModule>();
    }
}
