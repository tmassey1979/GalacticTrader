namespace GalacticTrader.Desktop.Starmap;

public static class StarmapSceneResolver
{
    public static async Task<StarmapLoadResult> ResolveAsync(
        Func<CancellationToken, Task<StarmapScene>> primaryLoader,
        Func<StarmapScene> fallbackFactory,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var primary = await primaryLoader(cancellationToken);
            return new StarmapLoadResult
            {
                Scene = primary,
                UsedFallback = false
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return new StarmapLoadResult
            {
                Scene = fallbackFactory(),
                UsedFallback = true,
                Warning = $"Database starmap load failed: {exception.Message}. Running procedural fallback scene."
            };
        }
    }
}
