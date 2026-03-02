using System.Windows;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Starmap;

public sealed class OrbitCameraController
{
    private readonly Point3D _defaultFocusCenter = new(0, 0, 0);

    public double YawDegrees { get; private set; } = 48;
    public double PitchDegrees { get; private set; } = -24;
    public double Distance { get; private set; } = 220;
    public Point3D FocusCenter { get; private set; } = new(0, 0, 0);

    public void OrbitBy(Vector delta)
    {
        YawDegrees = (YawDegrees + (delta.X * 0.35)) % 360;
        PitchDegrees = Math.Clamp(PitchDegrees - (delta.Y * 0.28), -80, -5);
    }

    public void ZoomBy(int mouseWheelDelta)
    {
        Distance = Math.Clamp(Distance - (mouseWheelDelta * 0.06), 60, 420);
    }

    public void Reset()
    {
        FocusCenter = _defaultFocusCenter;
        YawDegrees = 48;
        PitchDegrees = -24;
        Distance = 220;
    }

    public void FocusOnRoute(RouteSegment route)
    {
        FocusCenter = new Point3D(
            (route.From.X + route.To.X) / 2,
            (route.From.Y + route.To.Y) / 2,
            (route.From.Z + route.To.Z) / 2);

        YawDegrees = (YawDegrees + 6) % 360;
        Distance = Math.Max(90, Distance - 10);
    }

    public void SetFromSliders(double yawDegrees, double pitchDegrees, double distance)
    {
        YawDegrees = yawDegrees;
        PitchDegrees = pitchDegrees;
        Distance = distance;
    }

    public CameraPose BuildPose()
    {
        var yawRadians = YawDegrees * Math.PI / 180;
        var pitchRadians = PitchDegrees * Math.PI / 180;

        var x = FocusCenter.X + (Distance * Math.Cos(pitchRadians) * Math.Cos(yawRadians));
        var y = FocusCenter.Y + (Distance * Math.Sin(pitchRadians));
        var z = FocusCenter.Z + (Distance * Math.Cos(pitchRadians) * Math.Sin(yawRadians));

        var position = new Point3D(x, y, z);
        var lookDirection = new Vector3D(FocusCenter.X - x, FocusCenter.Y - y, FocusCenter.Z - z);
        var upDirection = new Vector3D(0, 1, 0);
        return new CameraPose(position, lookDirection, upDirection);
    }
}
