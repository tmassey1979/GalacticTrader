namespace GalacticTrader.ClientSdk.Dashboard;

public sealed record DashboardSnapshot(
    decimal AvailableCredits,
    int ShipCount,
    int FleetStrength,
    int EscortStrength,
    int DangerousRouteCount,
    int ActiveIntelligenceCount,
    int ReputationScore,
    decimal EconomicStabilityIndex,
    int ActivePlayers24h);
