using GalacticTrader.ClientSdk.Starmap;

namespace GalacticTrader.ClientSdk.Routes;

public sealed record RouteOverlayState(
    IReadOnlyList<StarmapRouteEdge> PlannedEdges,
    IReadOnlyList<StarmapRouteEdge> DangerousEdges,
    IReadOnlyList<StarmapRouteEdge> SuggestedEdges);
