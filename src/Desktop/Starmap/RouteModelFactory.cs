using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class RouteModelFactory
{
    public static GeometryModel3D Create(RouteSegment route)
    {
        var direction = route.To - route.From;
        var length = direction.Length;
        if (length <= 0.001)
        {
            return new GeometryModel3D();
        }

        var normalized = direction;
        normalized.Normalize();

        var baseDirection = new Vector3D(0, 0, 1);
        var rotationAxis = Vector3D.CrossProduct(baseDirection, normalized);
        var angle = Vector3D.AngleBetween(baseDirection, normalized);

        if (rotationAxis.LengthSquared < 0.00001)
        {
            rotationAxis = new Vector3D(0, 1, 0);
        }
        else
        {
            rotationAxis.Normalize();
        }

        var width = route.IsHighRisk ? 0.9 : 0.55;
        var mesh = MeshFactory.CreateUnitCubeMesh();
        var color = route.IsHighRisk ? Color.FromRgb(255, 124, 88) : Color.FromRgb(92, 168, 255);
        var material = new DiffuseMaterial(new SolidColorBrush(color)
        {
            Opacity = route.IsHighRisk ? 0.92 : 0.75
        });

        var midpoint = new Point3D(
            (route.From.X + route.To.X) / 2,
            (route.From.Y + route.To.Y) / 2,
            (route.From.Z + route.To.Z) / 2);

        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(width, width, length));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(rotationAxis, angle)));
        transform.Children.Add(new TranslateTransform3D(midpoint.X, midpoint.Y, midpoint.Z));

        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = transform
        };
    }
}
