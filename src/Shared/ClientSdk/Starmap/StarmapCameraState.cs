namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct StarmapCameraState(
    MapPoint3 Position,
    double ViewDistance,
    MapPoint3 Forward = default,
    double HorizontalFieldOfViewDegrees = 120d)
{
}
