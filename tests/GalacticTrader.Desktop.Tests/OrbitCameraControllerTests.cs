using GalacticTrader.Desktop.Starmap;
using System.Windows;

namespace GalacticTrader.Desktop.Tests;

public sealed class OrbitCameraControllerTests
{
    [Fact]
    public void OrbitBy_AndZoomBy_ClampExpectedRanges()
    {
        var controller = new OrbitCameraController();

        controller.OrbitBy(new Vector(1000, 1000));
        controller.ZoomBy(100_000);

        Assert.InRange(controller.PitchDegrees, -80, -5);
        Assert.InRange(controller.Distance, 60, 420);
    }

    [Fact]
    public void FocusOnRoute_MovesFocusCenterAndAdjustsDistance()
    {
        var controller = new OrbitCameraController();
        var route = new RouteSegment(
            "A -> B",
            From: new System.Windows.Media.Media3D.Point3D(0, 0, 0),
            To: new System.Windows.Media.Media3D.Point3D(10, 20, 30),
            IsHighRisk: false);

        controller.FocusOnRoute(route);

        Assert.Equal(5, controller.FocusCenter.X);
        Assert.Equal(10, controller.FocusCenter.Y);
        Assert.Equal(15, controller.FocusCenter.Z);
        Assert.True(controller.Distance <= 220);
    }
}
