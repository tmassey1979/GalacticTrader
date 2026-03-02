using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public sealed record RouteSegment(string Name, Point3D From, Point3D To, bool IsHighRisk);
