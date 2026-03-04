namespace GalacticTrader.ClientSdk.Routes;

public sealed record RouteRiskSimulation(
    double BaseRiskScore,
    double ProjectedRiskScore,
    double PiratePressure,
    double EconomicDensity,
    double InterdictionChance,
    decimal ProtectionCostEstimateCredits,
    int ExpectedTravelTimeSeconds,
    RouteRiskBand RiskBand);
