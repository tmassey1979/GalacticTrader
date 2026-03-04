using GalacticTrader.Desktop.Api;

namespace GalacticTrader.ClientSdk.Routes;

public sealed record RoutePlanningResult(
    RoutePlanApiDto Plan,
    RouteWaypointParseResult WaypointParse,
    RouteRiskSimulation RiskSimulation,
    RouteOverlayState Overlay);
