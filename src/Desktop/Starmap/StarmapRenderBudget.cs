namespace GalacticTrader.Desktop.Starmap;

public readonly record struct StarmapRenderBudget(
    int MaxRenderedStars,
    int MaxRenderedRoutes)
{
    public static StarmapRenderBudget Unlimited { get; } = new(int.MaxValue, int.MaxValue);

    public static StarmapRenderBudget StartupDefault { get; } = new(480, 1200);

    public static StarmapRenderBudget FromEnvironment()
    {
        var defaultBudget = StartupDefault;
        var maxRenderedStars = ResolveLimit("GT_STARMAP_STARTUP_MAX_STARS", defaultBudget.MaxRenderedStars);
        var maxRenderedRoutes = ResolveLimit("GT_STARMAP_STARTUP_MAX_ROUTES", defaultBudget.MaxRenderedRoutes);
        return new StarmapRenderBudget(maxRenderedStars, maxRenderedRoutes);
    }

    private static int ResolveLimit(string variableName, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(variableName);
        return int.TryParse(raw, out var parsed) && parsed > 0
            ? parsed
            : fallback;
    }
}
