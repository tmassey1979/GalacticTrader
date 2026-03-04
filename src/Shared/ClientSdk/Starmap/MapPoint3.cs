namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct MapPoint3(double X, double Y, double Z)
{
    public double DistanceSquaredTo(MapPoint3 other)
    {
        var deltaX = X - other.X;
        var deltaY = Y - other.Y;
        var deltaZ = Z - other.Z;
        return (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
    }
}
