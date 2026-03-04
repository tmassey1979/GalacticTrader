namespace GalacticTrader.ClientSdk.Routes;

public sealed record RouteWaypointParseResult(
    IReadOnlyList<RouteSectorOption> Waypoints,
    IReadOnlyList<string> UnmatchedTokens)
{
    public static RouteWaypointParseResult Empty { get; } = new([], []);

    public bool HasWaypoints => Waypoints.Count > 0;
}
