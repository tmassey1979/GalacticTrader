using GalacticTrader.Desktop.Assets;
using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Splash;

public static class SplashShipFactory
{
    private static readonly Lazy<Model3D?> ExternalSplashShip = new(() =>
    {
        var loader = new ExternalModelLoader();
        return loader.TryLoad(ExternalModelCatalog.SplashShip);
    });

    public static IReadOnlyList<Model3D> CreateShipModels()
    {
        var importedShip = ExternalSplashShip.Value;
        if (importedShip is not null)
        {
            var shipClone = importedShip.Clone();
            var transform = new Transform3DGroup();
            transform.Children.Add(new ScaleTransform3D(64, 64, 64));
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -88)));
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -12)));
            shipClone.Transform = transform;

            return
            [
                shipClone
            ];
        }

        return CreateProceduralFallback();
    }

    private static IReadOnlyList<Model3D> CreateProceduralFallback()
    {
        return
        [
            BoxModelFactory.Create(
                center: new Point3D(0, 0, 0),
                sizeX: 28,
                sizeY: 5,
                sizeZ: 10,
                color: Color.FromRgb(128, 154, 196)),
            BoxModelFactory.Create(
                center: new Point3D(16, 0, 0),
                sizeX: 9,
                sizeY: 3.2,
                sizeZ: 6,
                color: Color.FromRgb(188, 210, 245)),
            BoxModelFactory.Create(
                center: new Point3D(8, 2.1, 0),
                sizeX: 8,
                sizeY: 1.6,
                sizeZ: 5,
                color: Color.FromRgb(97, 177, 229),
                opacity: 0.95),
            BoxModelFactory.Create(
                center: new Point3D(-2, -0.9, 8),
                sizeX: 20,
                sizeY: 1.4,
                sizeZ: 7,
                color: Color.FromRgb(106, 130, 171)),
            BoxModelFactory.Create(
                center: new Point3D(-2, -0.9, -8),
                sizeX: 20,
                sizeY: 1.4,
                sizeZ: 7,
                color: Color.FromRgb(106, 130, 171)),
            BoxModelFactory.Create(
                center: new Point3D(-16, -0.2, 3.8),
                sizeX: 4.3,
                sizeY: 2,
                sizeZ: 2.5,
                color: Color.FromRgb(95, 108, 139)),
            BoxModelFactory.Create(
                center: new Point3D(-16, -0.2, -3.8),
                sizeX: 4.3,
                sizeY: 2,
                sizeZ: 2.5,
                color: Color.FromRgb(95, 108, 139)),
            BoxModelFactory.Create(
                center: new Point3D(-18.4, -0.2, 3.8),
                sizeX: 0.9,
                sizeY: 1.4,
                sizeZ: 1.7,
                color: Color.FromRgb(118, 229, 248),
                opacity: 0.95),
            BoxModelFactory.Create(
                center: new Point3D(-18.4, -0.2, -3.8),
                sizeX: 0.9,
                sizeY: 1.4,
                sizeZ: 1.7,
                color: Color.FromRgb(118, 229, 248),
                opacity: 0.95)
        ];
    }
}
