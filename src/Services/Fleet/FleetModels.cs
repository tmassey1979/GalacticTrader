namespace GalacticTrader.Services.Fleet;

public enum FleetFormation
{
    Loose,
    Defensive,
    Spearhead
}

public sealed class ShipTemplateDto
{
    public string Key { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int ShieldCapacity { get; init; }
    public int ReactorOutput { get; init; }
    public int CargoCapacity { get; init; }
    public int SensorRange { get; init; }
    public int SignatureProfile { get; init; }
    public int CrewSlots { get; init; }
    public int Hardpoints { get; init; }
    public decimal PurchasePrice { get; init; }
}

public sealed class PurchaseShipRequest
{
    public Guid PlayerId { get; init; }
    public string TemplateKey { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
}

public sealed class InstallShipModuleRequest
{
    public Guid ShipId { get; init; }
    public string ModuleType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Tier { get; init; } = 1;
}

public sealed class ShipDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ShipClass { get; init; } = string.Empty;
    public int HullIntegrity { get; init; }
    public int MaxHullIntegrity { get; init; }
    public int ShieldCapacity { get; init; }
    public int MaxShieldCapacity { get; init; }
    public int ReactorOutput { get; init; }
    public int CargoCapacity { get; init; }
    public int SensorRange { get; init; }
    public int SignatureProfile { get; init; }
    public int CrewSlots { get; init; }
    public int Hardpoints { get; init; }
    public decimal CurrentValue { get; init; }
    public IReadOnlyList<ShipModuleDto> Modules { get; init; } = [];
    public int CrewCount { get; init; }
}

public sealed class ShipModuleDto
{
    public Guid Id { get; init; }
    public string ModuleType { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public int Tier { get; init; }
    public decimal PurchasePrice { get; init; }
}

public sealed class HireCrewRequest
{
    public Guid PlayerId { get; init; }
    public Guid? ShipId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public decimal Salary { get; init; }
}

public sealed class CrewProgressRequest
{
    public long ExperienceGained { get; init; }
    public int MissionOutcomeScore { get; init; }
}

public sealed class CrewMemberDto
{
    public Guid Id { get; init; }
    public Guid PlayerId { get; init; }
    public Guid? ShipId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public int CombatSkill { get; init; }
    public int EngineeringSkill { get; init; }
    public int NavigationSkill { get; init; }
    public int Morale { get; init; }
    public int Loyalty { get; init; }
    public int ExperienceLevel { get; init; }
    public long ExperiencePoints { get; init; }
}

public sealed class CrewImpactModifiersDto
{
    public float CombatModifier { get; init; }
    public float EngineeringModifier { get; init; }
    public float NavigationModifier { get; init; }
    public float MoraleFactor { get; init; }
    public float LoyaltyFactor { get; init; }
}

public sealed class EscortSummaryDto
{
    public Guid PlayerId { get; init; }
    public int FleetStrength { get; init; }
    public int EscortStrength { get; init; }
    public float ConvoyBonus { get; init; }
    public FleetFormation Formation { get; init; }
    public float ProtectiveRange { get; init; }
    public float CoordinationBonus { get; init; }
    public float CombatModifier { get; init; }
    public CrewImpactModifiersDto CrewImpact { get; init; } = new();
}

public sealed class ConvoySimulationRequest
{
    public Guid PlayerId { get; init; }
    public FleetFormation Formation { get; init; } = FleetFormation.Defensive;
    public decimal ConvoyValue { get; init; }
}

public sealed class ConvoySimulationResult
{
    public EscortSummaryDto Summary { get; init; } = new();
    public int ExpectedLossPercent { get; init; }
    public decimal ProjectedProtectedValue { get; init; }
}
