using System;

namespace GalacticTrader.Data.Models
{
    /// <summary>
    /// Represents a crew member
    /// </summary>
    public class Crew
    {
        public Guid Id { get; set; }
        public Guid PlayerId { get; set; }
        public Guid? ShipId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        
        // Skills (0-100)
        public int CombatSkill { get; set; }
        public int EngineeringSkill { get; set; }
        public int NavigationSkill { get; set; }
        
        // Status
        public int Morale { get; set; } // 0-100
        public int Loyalty { get; set; } // 0-100
        public decimal Salary { get; set; }
        
        // Experience
        public int ExperienceLevel { get; set; }
        public long ExperiencePoints { get; set; }
        
        // Employment
        public DateTime HiredAt { get; set; }
        public bool IsActive { get; set; }
        
        // Navigation
        public Player Player { get; set; } = null!;
        public Ship? Ship { get; set; }
    }
}
