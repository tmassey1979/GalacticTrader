using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Routes;

public sealed record RouteOptimizationView(
    RouteOptimizationApiDto Optimization,
    RouteOptimizationProfile RecommendedProfile,
    RouteRiskSimulation? RecommendedRiskSimulation,
    RouteOverlayState Overlay);
