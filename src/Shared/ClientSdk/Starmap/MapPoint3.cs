namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct MapPoint3(double X, double Y, double Z)
{
    public double MagnitudeSquared => (X * X) + (Y * Y) + (Z * Z);

    public double Magnitude => Math.Sqrt(MagnitudeSquared);

    public double DistanceSquaredTo(MapPoint3 other)
    {
        var deltaX = X - other.X;
        var deltaY = Y - other.Y;
        var deltaZ = Z - other.Z;
        return (deltaX * deltaX) + (deltaY * deltaY) + (deltaZ * deltaZ);
    }

    public static MapPoint3 Normalize(MapPoint3 vector)
    {
        var magnitude = vector.Magnitude;
        if (magnitude <= double.Epsilon)
        {
            return new MapPoint3(0d, 0d, 0d);
        }

        return new MapPoint3(
            X: vector.X / magnitude,
            Y: vector.Y / magnitude,
            Z: vector.Z / magnitude);
    }

    public static double Dot(MapPoint3 left, MapPoint3 right)
    {
        return (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);
    }

    public static MapPoint3 operator -(MapPoint3 left, MapPoint3 right)
    {
        return new MapPoint3(
            X: left.X - right.X,
            Y: left.Y - right.Y,
            Z: left.Z - right.Z);
    }
}
