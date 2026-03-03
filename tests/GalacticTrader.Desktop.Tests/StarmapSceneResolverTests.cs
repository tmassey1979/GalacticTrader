using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapSceneResolverTests
{
    [Fact]
    public async Task ResolveAsync_ReturnsPrimaryScene_WhenPrimaryLoaderSucceeds()
    {
        var primaryScene = new StarmapScene([], [], new Model3DGroup());
        var fallbackInvoked = false;

        var result = await StarmapSceneResolver.ResolveAsync(
            _ => Task.FromResult(primaryScene),
            () =>
            {
                fallbackInvoked = true;
                return new StarmapScene([], [], new Model3DGroup());
            });

        Assert.False(result.UsedFallback);
        Assert.False(fallbackInvoked);
        Assert.Same(primaryScene, result.Scene);
        Assert.True(string.IsNullOrEmpty(result.Warning));
    }

    [Fact]
    public async Task ResolveAsync_UsesFallback_WhenPrimaryLoaderThrows()
    {
        var fallbackScene = new StarmapScene([], [], new Model3DGroup());

        var result = await StarmapSceneResolver.ResolveAsync(
            _ => throw new InvalidOperationException("api unavailable"),
            () => fallbackScene);

        Assert.True(result.UsedFallback);
        Assert.Same(fallbackScene, result.Scene);
        Assert.Contains("api unavailable", result.Warning, StringComparison.Ordinal);
    }
}
