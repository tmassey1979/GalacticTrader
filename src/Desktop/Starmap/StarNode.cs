using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public sealed record StarNode(string Name, Point3D Position, double Magnitude, bool IsHub);
