using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Rendering;

public static class BoxModelFactory
{
    public static GeometryModel3D Create(
        Point3D center,
        double sizeX,
        double sizeY,
        double sizeZ,
        Color color,
        double opacity = 1)
    {
        var mesh = MeshFactory.CreateUnitCubeMesh();
        var brush = new SolidColorBrush(color)
        {
            Opacity = opacity
        };
        brush.Freeze();

        var material = new DiffuseMaterial(brush);
        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(sizeX, sizeY, sizeZ));
        transform.Children.Add(new TranslateTransform3D(center.X, center.Y, center.Z));

        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = transform
        };
    }
}
