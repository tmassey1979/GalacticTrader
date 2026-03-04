namespace GalacticTrader.ClientSdk.Fleet;

public sealed record FleetStatusSummary(
    int ShipCount,
    int CrewCount,
    int CrewCapacity,
    int InsuredShipCount,
    decimal FleetValue,
    double AverageHullIntegrityPercent,
    int FleetStrength,
    string ProtectionStatus,
    float EscortCoordinationBonus);
