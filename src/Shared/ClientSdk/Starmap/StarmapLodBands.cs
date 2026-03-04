namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct StarmapLodBands(double NearDistance, double MidDistance)
{
    public static StarmapLodBands StartupDefault { get; } = new(NearDistance: 80d, MidDistance: 220d);

    public StarmapLodTier Resolve(double distanceFromCamera)
    {
        if (distanceFromCamera <= NearDistance)
        {
            return StarmapLodTier.Near;
        }

        if (distanceFromCamera <= MidDistance)
        {
            return StarmapLodTier.Mid;
        }

        return StarmapLodTier.Far;
    }
}
