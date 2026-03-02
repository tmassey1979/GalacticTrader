using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace GalacticTrader.Desktop;

public partial class SplashWindow : Window
{
    private readonly TaskCompletionSource _playCompletion = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly DispatcherTimer _terminalTimer;
    private readonly IReadOnlyList<string> _bootLines =
    [
        "[core] bootstrapping starship command runtime",
        "[vault] initializing secure secrets channel",
        "[nav] synchronizing stellar route graph",
        "[fleet] loading tactical ship manifests",
        "[trade] calibrating market volatility indexes",
        "[comms] opening encrypted relay channels",
        "[ready] command interface online"
    ];

    private int _bootLineIndex;

    public SplashWindow()
    {
        InitializeComponent();
        BuildSplashScene();

        _terminalTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };
        _terminalTimer.Tick += OnTerminalTick;

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public Task PlayAsync() => _playCompletion.Task;

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _terminalTimer.Start();

        if (FindResource("SplashStoryboard") is not Storyboard storyboardTemplate)
        {
            _playCompletion.TrySetResult();
            return;
        }

        var storyboard = storyboardTemplate.Clone();
        storyboard.Completed += (_, _) =>
        {
            LoadingProgress.Value = 100;
            _terminalTimer.Stop();
            _playCompletion.TrySetResult();
        };

        storyboard.Begin(this);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _terminalTimer.Stop();
    }

    private void OnTerminalTick(object? sender, EventArgs e)
    {
        if (_bootLineIndex >= _bootLines.Count)
        {
            _terminalTimer.Stop();
            return;
        }

        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        TerminalLog.Text += $"{timestamp}  {_bootLines[_bootLineIndex]}{Environment.NewLine}";
        _bootLineIndex++;
    }

    private void BuildSplashScene()
    {
        BuildStarfield();
        BuildShipModel();
    }

    private void BuildStarfield()
    {
        var random = new Random(2001);
        var modelGroup = new Model3DGroup();

        for (var index = 0; index < 180; index++)
        {
            var x = random.NextDouble() * 240 - 120;
            var y = random.NextDouble() * 100 - 50;
            var z = -(random.NextDouble() * 260 + 60);
            var size = random.NextDouble() * 0.75 + 0.2;
            var brightness = (byte)(190 + random.Next(60));
            var color = Color.FromRgb(brightness, brightness, 255);

            modelGroup.Children.Add(CreateBoxModel(
                center: new Point3D(x, y, z),
                sizeX: size,
                sizeY: size,
                sizeZ: size,
                color: color,
                opacity: 0.9));
        }

        StarfieldVisual.Content = modelGroup;
    }

    private void BuildShipModel()
    {
        ShipModelGroup.Children.Clear();

        // Main fuselage.
        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(0, 0, 0),
            sizeX: 28,
            sizeY: 5,
            sizeZ: 10,
            color: Color.FromRgb(128, 154, 196)));

        // Nose and cockpit.
        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(16, 0, 0),
            sizeX: 9,
            sizeY: 3.2,
            sizeZ: 6,
            color: Color.FromRgb(188, 210, 245)));

        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(8, 2.1, 0),
            sizeX: 8,
            sizeY: 1.6,
            sizeZ: 5,
            color: Color.FromRgb(97, 177, 229),
            opacity: 0.95));

        // Wings.
        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-2, -0.9, 8),
            sizeX: 20,
            sizeY: 1.4,
            sizeZ: 7,
            color: Color.FromRgb(106, 130, 171)));

        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-2, -0.9, -8),
            sizeX: 20,
            sizeY: 1.4,
            sizeZ: 7,
            color: Color.FromRgb(106, 130, 171)));

        // Engines and glow.
        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-16, -0.2, 3.8),
            sizeX: 4.3,
            sizeY: 2,
            sizeZ: 2.5,
            color: Color.FromRgb(95, 108, 139)));

        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-16, -0.2, -3.8),
            sizeX: 4.3,
            sizeY: 2,
            sizeZ: 2.5,
            color: Color.FromRgb(95, 108, 139)));

        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-18.4, -0.2, 3.8),
            sizeX: 0.9,
            sizeY: 1.4,
            sizeZ: 1.7,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.95));

        ShipModelGroup.Children.Add(CreateBoxModel(
            center: new Point3D(-18.4, -0.2, -3.8),
            sizeX: 0.9,
            sizeY: 1.4,
            sizeZ: 1.7,
            color: Color.FromRgb(118, 229, 248),
            opacity: 0.95));
    }

    private static GeometryModel3D CreateBoxModel(
        Point3D center,
        double sizeX,
        double sizeY,
        double sizeZ,
        Color color,
        double opacity = 1)
    {
        var mesh = CreateUnitCubeMesh();
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
}
