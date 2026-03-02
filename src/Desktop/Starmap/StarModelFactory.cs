using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class StarModelFactory
{
    public static GeometryModel3D Create(StarNode star)
    {
        var radius = star.IsHub ? 2.5 : 1.4 + (star.Magnitude / 3.5);
        var mesh = MeshFactory.CreateSphereMesh(radius, 18, 14);
        var color = star.IsHub ? Color.FromRgb(255, 230, 168) : Color.FromRgb(179, 208, 255);

        var material = new DiffuseMaterial(new SolidColorBrush(color));
        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = new TranslateTransform3D(star.Position.X, star.Position.Y, star.Position.Z)
        };
    }
}
