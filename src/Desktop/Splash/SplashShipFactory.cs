using GalacticTrader.Desktop.Rendering;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Splash;

public static class SplashShipFactory
{
    public static IReadOnlyList<Model3D> CreateShipModels()
    {
        return
        [
            CreateShip(BuildCommandShip(), CreateCommandShipTransform()),
            CreateShip(BuildInterceptorShip(), CreateEscortTransform(24, 8, -30, -74, -5, 0.11)),
            CreateShip(BuildHaulerShip(), CreateEscortTransform(-28, -6, 20, -102, -8, 0.12))
        ];
    }

    private static Model3D CreateShip(Model3DGroup shipMesh, Transform3D transform)
    {
        shipMesh.Transform = transform;
        return shipMesh;
    }

    private static Model3DGroup BuildCommandShip()
    {
        var ship = new Model3DGroup();
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(0, 0, 0),
            sizeX: 28,
            sizeY: 5,
            sizeZ: 10,
            color: Color.FromRgb(128, 154, 196)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(16, 0, 0),
            sizeX: 9,
            sizeY: 3.2,
            sizeZ: 6,
            color: Color.FromRgb(188, 210, 245)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(8, 2.1, 0),
            sizeX: 8,
            sizeY: 1.6,
            sizeZ: 5,
            color: Color.FromRgb(97, 177, 229),
            opacity: 0.95));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-2, -0.9, 8),
            sizeX: 20,
            sizeY: 1.4,
            sizeZ: 7,
            color: Color.FromRgb(106, 130, 171)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-2, -0.9, -8),
            sizeX: 20,
            sizeY: 1.4,
            sizeZ: 7,
            color: Color.FromRgb(106, 130, 171)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-16, -0.2, 3.8),
            sizeX: 4.3,
            sizeY: 2,
            sizeZ: 2.5,
            color: Color.FromRgb(95, 108, 139)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-16, -0.2, -3.8),
            sizeX: 4.3,
            sizeY: 2,
            sizeZ: 2.5,
            color: Color.FromRgb(95, 108, 139)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-18.4, -0.2, 3.8),
            sizeX: 0.9,
            sizeY: 1.4,
            sizeZ: 1.7,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.95));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-18.4, -0.2, -3.8),
            sizeX: 0.9,
            sizeY: 1.4,
            sizeZ: 1.7,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.95));
        return ship;
    }

    private static Model3DGroup BuildInterceptorShip()
    {
        var ship = new Model3DGroup();
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(0, 0, 0),
            sizeX: 18,
            sizeY: 3.2,
            sizeZ: 8,
            color: Color.FromRgb(105, 138, 184)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(9, 0, 0),
            sizeX: 7,
            sizeY: 2.4,
            sizeZ: 4.5,
            color: Color.FromRgb(177, 204, 239)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-2, -0.8, 7),
            sizeX: 16,
            sizeY: 1,
            sizeZ: 3,
            color: Color.FromRgb(92, 114, 150)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-2, -0.8, -7),
            sizeX: 16,
            sizeY: 1,
            sizeZ: 3,
            color: Color.FromRgb(92, 114, 150)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-12.5, 0, 0),
            sizeX: 2.2,
            sizeY: 1.8,
            sizeZ: 4.4,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.9));
        return ship;
    }

    private static Model3DGroup BuildHaulerShip()
    {
        var ship = new Model3DGroup();
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(0, 0, 0),
            sizeX: 24,
            sizeY: 5.6,
            sizeZ: 11,
            color: Color.FromRgb(121, 142, 168)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(12.4, 0.5, 0),
            sizeX: 6.8,
            sizeY: 3,
            sizeZ: 5.4,
            color: Color.FromRgb(173, 197, 230)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-3, 3, 0),
            sizeX: 8,
            sizeY: 1.5,
            sizeZ: 5.2,
            color: Color.FromRgb(90, 165, 221),
            opacity: 0.92));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-8.5, -1.9, 5.9),
            sizeX: 8.6,
            sizeY: 2.2,
            sizeZ: 3.8,
            color: Color.FromRgb(96, 109, 137)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-8.5, -1.9, -5.9),
            sizeX: 8.6,
            sizeY: 2.2,
            sizeZ: 3.8,
            color: Color.FromRgb(96, 109, 137)));
        ship.Children.Add(BoxModelFactory.Create(
            center: new Point3D(-14.2, -1.2, 0),
            sizeX: 3,
            sizeY: 2.3,
            sizeZ: 5.2,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.9));
        return ship;
    }

    private static Transform3D CreateCommandShipTransform()
    {
        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(0.125, 0.125, 0.125));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -88)));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -12)));
        return transform;
    }

    private static Transform3D CreateEscortTransform(
        double offsetX,
        double offsetY,
        double offsetZ,
        double yaw,
        double pitch,
        double scale)
    {
        var transform = new Transform3DGroup();
        transform.Children.Add(new ScaleTransform3D(scale, scale, scale));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw)));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitch)));
        transform.Children.Add(new TranslateTransform3D(offsetX, offsetY, offsetZ));
        return transform;
    }
}
