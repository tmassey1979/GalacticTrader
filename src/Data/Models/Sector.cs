using System;
using System.Collections.Generic;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a sector in space - nodes in the graph
    /// </summary>
    public class Sector
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        
        // Coordinates for rendering
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        
        // Danger & Resources
        public int SecurityLevel { get; set; } // 0-100, higher = safer
        public int HazardRating { get; set; } // 0-100, higher = more dangerous
        public float ResourceModifier { get; set; } // 0.5-2.0 price modifier
        
        // Economic & Strategic
        public int EconomicIndex { get; set; } // 0-100, higher = more trading
        public float SensorInterferenceLevel { get; set; } // 0-100
        public Guid? ControlledByFactionId { get; set; }
        
        // Traffic & Activity
        public int AverageTrafficLevel { get; set; } // 0-100
        public int PiratePresenceProbability { get; set; } // 0-100
        
        // Navigation
        public ICollection<Route> OutboundRoutes { get; set; } = new List<Route>();
        public ICollection<Route> InboundRoutes { get; set; } = new List<Route>();
        public ICollection<Market> Markets { get; set; } = new List<Market>();
        public ICollection<Ship> DockedShips { get; set; } = new List<Ship>();
        public Faction ControlledByFaction { get; set; }
    }
}
