using GalacticTrader.Desktop.Assets;
using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public static class StarModelFactory
{
    private static readonly Lazy<Model3D?> ExternalStarBody = new(() =>
    {
        var loader = new ExternalModelLoader();
        return loader.TryLoad(ExternalModelCatalog.StarBody);
    });

    public static Model3D Create(StarNode star)
    {
        var importedBody = ExternalStarBody.Value;
        if (importedBody is not null)
        {
            var starBody = importedBody.Clone();
            var scale = star.IsHub ? 14d : 8d + (star.Magnitude * 3d);
            var transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(scale, scale, scale));
            transform.Children.Add(new TranslateTransform3D(star.Position.X, star.Position.Y, star.Position.Z));
            starBody.Transform = transform;
            return starBody;
        }

        return CreateProceduralFallback(star);
    }

    private static GeometryModel3D CreateProceduralFallback(StarNode star)
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
