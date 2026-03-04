namespace GalacticTrader.ClientSdk.Starmap;

public readonly record struct StarmapRenderBudget(
    int MaxRenderedSectors,
    int MaxRenderedRoutes,
    int MaxActiveChunks)
{
    public static StarmapRenderBudget Unlimited { get; } = new(
        MaxRenderedSectors: int.MaxValue,
        MaxRenderedRoutes: int.MaxValue,
        MaxActiveChunks: int.MaxValue);

    public static StarmapRenderBudget StartupDefault { get; } = new(
        MaxRenderedSectors: 480,
        MaxRenderedRoutes: 1200,
        MaxActiveChunks: 125);
}
