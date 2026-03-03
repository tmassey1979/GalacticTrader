using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Splash;

public static class SplashSceneFactory
{
    public static Model3DGroup CreateBackdropModels()
    {
        var group = new Model3DGroup();
        group.Children.Add(CreatePlanet(
            center: new Point3D(-66, 26, -218),
            radius: 15,
            baseColor: Color.FromRgb(91, 132, 196),
            atmosphereColor: Color.FromRgb(147, 204, 255),
            atmosphereOpacity: 0.3));
        group.Children.Add(CreateRingPlanet(
            center: new Point3D(78, -18, -188),
            radius: 12,
            baseColor: Color.FromRgb(187, 132, 89),
            cloudColor: Color.FromRgb(230, 198, 156),
            ringColor: Color.FromRgb(227, 208, 173)));
        group.Children.Add(CreatePlanet(
            center: new Point3D(8, 40, -270),
            radius: 7.5,
            baseColor: Color.FromRgb(94, 108, 158),
            atmosphereColor: Color.FromRgb(163, 190, 246),
            atmosphereOpacity: 0.24));
        return group;
    }

    private static Model3DGroup CreatePlanet(
        Point3D center,
        double radius,
        Color baseColor,
        Color atmosphereColor,
        double atmosphereOpacity)
    {
        var planet = new Model3DGroup();
        planet.Children.Add(CreateSphere(center, radius, baseColor, 1));
        planet.Children.Add(CreateSphere(center, radius * 1.06, atmosphereColor, atmosphereOpacity));
        return planet;
    }

    private static Model3DGroup CreateRingPlanet(
        Point3D center,
        double radius,
        Color baseColor,
        Color cloudColor,
        Color ringColor)
    {
        var planet = new Model3DGroup();
        planet.Children.Add(CreateSphere(center, radius, baseColor, 1));
        planet.Children.Add(CreateSphere(center, radius * 1.04, cloudColor, 0.26));
        planet.Children.Add(CreateRing(center, radius * 1.62, ringColor));
        return planet;
    }

    private static GeometryModel3D CreateSphere(
        Point3D center,
        double radius,
        Color color,
        double opacity)
    {
        var mesh = MeshFactory.CreateSphereMesh(radius, thetaDiv: 24, phiDiv: 16);
        var brush = new SolidColorBrush(color)
        {
            Opacity = opacity
        };
        brush.Freeze();

        var material = new DiffuseMaterial(brush);
        return new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = new TranslateTransform3D(center.X, center.Y, center.Z)
        };
    }

    private static Model3DGroup CreateRing(Point3D center, double radius, Color color)
    {
        var ring = new Model3DGroup();
        const int segmentCount = 40;
        const double thickness = 0.34;
        const double depth = 1.6;

        for (var segment = 0; segment < segmentCount; segment++)
        {
            var angle = (Math.PI * 2 * segment) / segmentCount;
            var x = center.X + (radius * Math.Cos(angle));
            var z = center.Z + (radius * Math.Sin(angle));

            var piece = BoxModelFactory.Create(
                center: new Point3D(x, center.Y, z),
                sizeX: depth,
                sizeY: thickness,
                sizeZ: depth,
                color: color,
                opacity: 0.52);

            piece.Transform = new Transform3DGroup
            {
                Children =
                {
                    piece.Transform,
                    new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 68), center)
                }
            };

            ring.Children.Add(piece);
        }

        return ring;
    }
}
