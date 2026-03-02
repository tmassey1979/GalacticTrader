using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public readonly record struct CameraPose(Point3D Position, Vector3D LookDirection, Vector3D UpDirection);
