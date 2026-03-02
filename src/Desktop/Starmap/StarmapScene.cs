using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public sealed record StarmapScene(
    IReadOnlyList<StarNode> Stars,
    IReadOnlyList<RouteSegment> Routes,
    Model3DGroup Models);
