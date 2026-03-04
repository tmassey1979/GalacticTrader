using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a route (edge) between two sectors
    /// </summary>
    public class Route
    {
        public Guid Id { get; set; }
        public Guid FromSectorId { get; set; }
        public Guid ToSectorId { get; set; }
        
        // Travel Costs
        public int TravelTimeSeconds { get; set; }
        public float FuelCost { get; set; }
        public float BaseRiskScore { get; set; } // 0-100
        
        // Navigation
        public float VisibilityRating { get; set; } // 0-100, higher = easier to detect
        public string LegalStatus { get; set; } = string.Empty; // "legal", "gray", "black"
        public string WarpGateType { get; set; } = string.Empty; // "standard", "unstable", etc.
        
        // Route Properties
        public bool IsDiscovered { get; set; }
        public bool HasAnomalies { get; set; }
        public int TrafficIntensity { get; set; } // 0-100
        
        // Navigation
        public Sector FromSector { get; set; } = null!;
        public Sector ToSector { get; set; } = null!;
    }
}
