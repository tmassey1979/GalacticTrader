using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Fleet;
using GalacticTrader.Desktop.Intel;
using GalacticTrader.Desktop.Starmap;
using GalacticTrader.Desktop.Trading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GalacticTrader.Desktop;

public partial class MainWindow : Window
{
    private readonly OrbitCameraController _cameraController = new();
    private readonly StarmapScene _scene;
    private readonly DesktopSession _session;
    private bool _isOrbiting;
    private Point _lastMousePoint;
    private bool _isUpdatingSliders;

    public MainWindow(
        StarmapScene scene,
        DesktopSession session,
        NavigationApiClient navigationApiClient,
        EconomyApiClient economyApiClient,
        MarketApiClient marketApiClient,
        FleetApiClient fleetApiClient,
        ReputationApiClient reputationApiClient,
        StrategicApiClient strategicApiClient)
    {
        _scene = scene;
        _session = session;
        InitializeComponent();
        BuildStarmap(_scene);
        TradingHost.Content = new TradingPanel(_session, economyApiClient, marketApiClient);
        FleetHost.Content = new FleetPanel(_session, fleetApiClient);
        IntelHost.Content = new IntelPanel(_session, navigationApiClient, reputationApiClient, strategicApiClient);
        ApplyCamera(updateSliders: true);

        if (!string.IsNullOrWhiteSpace(session.Username))
        {
            Title = $"Galactic Trader Command - {session.Username}";
        }

        StarViewport.MouseLeftButtonDown += OnViewportMouseLeftButtonDown;
        StarViewport.MouseLeftButtonUp += OnViewportMouseLeftButtonUp;
        StarViewport.MouseMove += OnViewportMouseMove;
        StarViewport.MouseWheel += OnViewportMouseWheel;
    }

    private void BuildStarmap(StarmapScene scene)
    {
        RouteList.ItemsSource = scene.Routes;
        SceneModels.Content = scene.Models;
    }

    private void ApplyCamera(bool updateSliders)
    {
        var pose = _cameraController.BuildPose();
        SceneCamera.Position = pose.Position;
        SceneCamera.LookDirection = pose.LookDirection;
        SceneCamera.UpDirection = pose.UpDirection;

        if (!updateSliders)
        {
            return;
        }

        _isUpdatingSliders = true;
        YawSlider.Value = _cameraController.YawDegrees;
        PitchSlider.Value = _cameraController.PitchDegrees;
        ZoomSlider.Value = _cameraController.Distance;
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

        _cameraController.OrbitBy(delta);
        ApplyCamera(updateSliders: true);
    }

    private void OnViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        _cameraController.ZoomBy(e.Delta);
        ApplyCamera(updateSliders: true);
    }

    private void OnCameraSliderChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isUpdatingSliders)
        {
            return;
        }

        _cameraController.SetFromSliders(
            yawDegrees: YawSlider.Value,
            pitchDegrees: PitchSlider.Value,
            distance: ZoomSlider.Value);
        ApplyCamera(updateSliders: false);
    }

    private void OnResetCameraClick(object sender, RoutedEventArgs e)
    {
        _cameraController.Reset();
        ApplyCamera(updateSliders: true);
    }

    private void OnRouteSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (RouteList.SelectedItem is not RouteSegment selected)
        {
            return;
        }

        _cameraController.FocusOnRoute(selected);
        ApplyCamera(updateSliders: true);
    }
}
