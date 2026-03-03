namespace GalacticTrader.Desktop.Fleet;

public sealed class FleetShipDisplayRow
{
    public required Guid ShipId { get; init; }
    public required string Name { get; init; }
    public required string ShipClass { get; init; }
    public int HullIntegrity { get; init; }
    public int MaxHullIntegrity { get; init; }
    public int CargoCapacity { get; init; }
    public int ModuleCount { get; init; }
    public decimal EconomicEfficiencyScore { get; init; }
    public decimal CrewSkillWeightScore { get; init; }
    public required string CrewSkillBand { get; init; }
    public required string UpgradePriority { get; init; }
    public required string UpgradeRecommendation { get; init; }
    public required string CrewAssignment { get; init; }
    public required string CrewStatus { get; init; }
    public required string InsuranceStatus { get; init; }
    public required string AssignedRoute { get; init; }
}
