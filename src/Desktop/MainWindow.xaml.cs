using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop;

public partial class MainWindow : Window
{
    private sealed record StarNode(string Name, Point3D Position, double Magnitude, bool IsHub);

    private sealed record RouteSegment(string Name, Point3D From, Point3D To, bool IsHighRisk);

    private readonly List<StarNode> _stars = [];
    private readonly List<RouteSegment> _routes = [];

    private Point3D _focusCenter = new(0, 0, 0);
    private bool _isOrbiting;
    private Point _lastMousePoint;
    private bool _isUpdatingSliders;

    private double _yawDegrees = 48;
    private double _pitchDegrees = -24;
    private double _distance = 220;

    public MainWindow()
    {
        InitializeComponent();
        BuildStarmap();
        ApplyCamera(updateSliders: true);

        StarViewport.MouseLeftButtonDown += OnViewportMouseLeftButtonDown;
        StarViewport.MouseLeftButtonUp += OnViewportMouseLeftButtonUp;
        StarViewport.MouseMove += OnViewportMouseMove;
        StarViewport.MouseWheel += OnViewportMouseWheel;
    }

    private void BuildStarmap()
    {
        _stars.AddRange(CreateStars());
        _routes.AddRange(CreateRoutes(_stars));

        RouteList.ItemsSource = _routes;

        var scene = new Model3DGroup();
        foreach (var route in _routes)
        {
            scene.Children.Add(CreateRouteModel(route));
        }

        foreach (var star in _stars)
        {
            scene.Children.Add(CreateStarModel(star));
        }

        SceneModels.Content = scene;
    }

    private static IEnumerable<StarNode> CreateStars()
    {
        var random = new Random(4242);
        var names = new[]
        {
            "Sol", "Aquila", "Vega", "Helios", "Nova", "Orion", "Horizon", "Zenith",
            "Aster", "Draco", "Lynx", "Hydra", "Arcadia", "Sirius", "Deneb", "Cetus",
            "Caelum", "Perseus", "Rigel", "Arcturus", "Lyra", "Altair", "Polaris", "Cygnus"
        };

        for (var i = 0; i < names.Length; i++)
        {
            var spiral = 26 + (i * 5.2);
            var angle = (i * 0.76) + (random.NextDouble() * 0.16);

            var x = Math.Cos(angle) * spiral + random.NextDouble() * 10 - 5;
            var y = random.NextDouble() * 80 - 40;
            var z = Math.Sin(angle) * spiral + random.NextDouble() * 10 - 5;
            var magnitude = 1.8 + random.NextDouble() * 3.2;
            var isHub = i % 6 == 0;

            yield return new StarNode(names[i], new Point3D(x, y, z), magnitude, isHub);
        }
    }

    private static IEnumerable<RouteSegment> CreateRoutes(IReadOnlyList<StarNode> stars)
    {
        var routes = new List<RouteSegment>();
        var seenPairs = new HashSet<string>(StringComparer.Ordinal);
        var random = new Random(7319);

        for (var i = 0; i < stars.Count; i++)
        {
            var current = stars[i];
            var nearest = stars
                .Where(s => !ReferenceEquals(s, current))
                .OrderBy(s => (s.Position - current.Position).LengthSquared)
                .Take(2)
                .ToList();

            foreach (var neighbor in nearest)
            {
                var pairKey = string.Compare(current.Name, neighbor.Name, StringComparison.Ordinal) < 0
                    ? $"{current.Name}:{neighbor.Name}"
                    : $"{neighbor.Name}:{current.Name}";

                if (!seenPairs.Add(pairKey))
                {
                    continue;
                }

                var distance = (neighbor.Position - current.Position).Length;
                var highRisk = distance > 52 || random.NextDouble() > 0.72;
                routes.Add(new RouteSegment(
                    Name: $"{current.Name} -> {neighbor.Name}",
                    From: current.Position,
                    To: neighbor.Position,
                    IsHighRisk: highRisk));
            }
        }

        return routes.OrderByDescending(route => (route.To - route.From).Length).Take(18);
    }

    private static GeometryModel3D CreateStarModel(StarNode star)
    {
        var radius = star.IsHub ? 2.5 : 1.4 + (star.Magnitude / 3.5);
        var mesh = CreateSphereMesh(radius, 18, 14);
        var color = star.IsHub ? Color.FromRgb(255, 230, 168) : Color.FromRgb(179, 208, 255);

        var material = new DiffuseMaterial(new SolidColorBrush(color));
        var model = new GeometryModel3D(mesh, material)
        {
            BackMaterial = material,
            Transform = new TranslateTransform3D(star.Position.X, star.Position.Y, star.Position.Z)
        };

        return model;
    }

    private static GeometryModel3D CreateRouteModel(RouteSegment route)
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
        var mesh = CreateUnitCubeMesh();
        var color = route.IsHighRisk ? Color.FromRgb(255, 124, 88) : Color.FromRgb(92, 168, 255);
        var material = new DiffuseMaterial(new SolidColorBrush(color) { Opacity = route.IsHighRisk ? 0.92 : 0.75 });

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

    private static MeshGeometry3D CreateUnitCubeMesh()
    {
        var mesh = new MeshGeometry3D();

        var points = new[]
        {
            new Point3D(-0.5, -0.5, -0.5),
            new Point3D(0.5, -0.5, -0.5),
            new Point3D(0.5, 0.5, -0.5),
            new Point3D(-0.5, 0.5, -0.5),
            new Point3D(-0.5, -0.5, 0.5),
            new Point3D(0.5, -0.5, 0.5),
            new Point3D(0.5, 0.5, 0.5),
            new Point3D(-0.5, 0.5, 0.5)
        };

        foreach (var point in points)
        {
            mesh.Positions.Add(point);
        }

        var indices = new[]
        {
            0, 2, 1, 0, 3, 2,
            4, 5, 6, 4, 6, 7,
            0, 1, 5, 0, 5, 4,
            2, 3, 7, 2, 7, 6,
            1, 2, 6, 1, 6, 5,
            3, 0, 4, 3, 4, 7
        };

        foreach (var index in indices)
        {
            mesh.TriangleIndices.Add(index);
        }

        return mesh;
    }

    private static MeshGeometry3D CreateSphereMesh(double radius, int thetaDiv, int phiDiv)
    {
        var mesh = new MeshGeometry3D();

        for (var phi = 0; phi <= phiDiv; phi++)
        {
            var phiRatio = (double)phi / phiDiv;
            var polar = Math.PI * phiRatio;
            var y = radius * Math.Cos(polar);
            var ringRadius = radius * Math.Sin(polar);

            for (var theta = 0; theta <= thetaDiv; theta++)
            {
                var thetaRatio = (double)theta / thetaDiv;
                var azimuth = 2 * Math.PI * thetaRatio;
                var x = ringRadius * Math.Cos(azimuth);
                var z = ringRadius * Math.Sin(azimuth);
                mesh.Positions.Add(new Point3D(x, y, z));
            }
        }

        var rowLength = thetaDiv + 1;
        for (var phi = 0; phi < phiDiv; phi++)
        {
            for (var theta = 0; theta < thetaDiv; theta++)
            {
                var current = (phi * rowLength) + theta;
                var next = current + rowLength;

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(next + 1);
                mesh.TriangleIndices.Add(next);

                mesh.TriangleIndices.Add(current);
                mesh.TriangleIndices.Add(current + 1);
                mesh.TriangleIndices.Add(next + 1);
            }
        }

        return mesh;
    }

    private void ApplyCamera(bool updateSliders)
    {
        var yawRadians = _yawDegrees * Math.PI / 180;
        var pitchRadians = _pitchDegrees * Math.PI / 180;

        var x = _focusCenter.X + (_distance * Math.Cos(pitchRadians) * Math.Cos(yawRadians));
        var y = _focusCenter.Y + (_distance * Math.Sin(pitchRadians));
        var z = _focusCenter.Z + (_distance * Math.Cos(pitchRadians) * Math.Sin(yawRadians));

        SceneCamera.Position = new Point3D(x, y, z);
        SceneCamera.LookDirection = new Vector3D(_focusCenter.X - x, _focusCenter.Y - y, _focusCenter.Z - z);
        SceneCamera.UpDirection = new Vector3D(0, 1, 0);

        if (!updateSliders)
        {
            return;
        }

        _isUpdatingSliders = true;
        YawSlider.Value = _yawDegrees;
        PitchSlider.Value = _pitchDegrees;
        ZoomSlider.Value = _distance;
        _isUpdatingSliders = false;
    }

    private void OnViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _isOrbiting = true;
        _lastMousePoint = e.GetPosition(StarViewport);
        StarViewport.CaptureMouse();
    }

    private void OnViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isOrbiting = false;
        StarViewport.ReleaseMouseCapture();
    }

    private void OnViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isOrbiting)
        {
            return;
        }

        var current = e.GetPosition(StarViewport);
        var delta = current - _lastMousePoint;
        _lastMousePoint = current;

        _yawDegrees = (_yawDegrees + (delta.X * 0.35)) % 360;
        _pitchDegrees = Math.Clamp(_pitchDegrees - (delta.Y * 0.28), -80, -5);
        ApplyCamera(updateSliders: true);
    }

    private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _distance = Math.Clamp(_distance - (e.Delta * 0.06), 60, 420);
        ApplyCamera(updateSliders: true);
    }

    private void OnCameraSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSliders)
        {
            return;
        }

        _yawDegrees = YawSlider.Value;
        _pitchDegrees = PitchSlider.Value;
        _distance = ZoomSlider.Value;
        ApplyCamera(updateSliders: false);
    }

    private void OnResetCameraClick(object sender, RoutedEventArgs e)
    {
        _focusCenter = new Point3D(0, 0, 0);
        _yawDegrees = 48;
        _pitchDegrees = -24;
        _distance = 220;
        ApplyCamera(updateSliders: true);
    }

    private void OnRouteSelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (RouteList.SelectedItem is not RouteSegment selected)
        {
            return;
        }

        var midpoint = new Point3D(
            (selected.From.X + selected.To.X) / 2,
            (selected.From.Y + selected.To.Y) / 2,
            (selected.From.Z + selected.To.Z) / 2);

        _focusCenter = midpoint;
        _yawDegrees = (_yawDegrees + 6) % 360;
        _distance = Math.Max(90, _distance - 10);
        ApplyCamera(updateSliders: true);
    }
}
