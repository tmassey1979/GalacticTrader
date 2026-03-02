using GalacticTrader.Desktop.Starmap;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapSceneBuilderTests
{
    [Fact]
    public void Build_CreatesDeterministicStarsRoutesAndModels()
    {
        var scene = StarmapSceneBuilder.Build();

        Assert.Equal(24, scene.Stars.Count);
        Assert.NotEmpty(scene.Routes);
        Assert.Equal(scene.Stars.Count + scene.Routes.Count, scene.Models.Children.Count);
        Assert.Equal("Sol", scene.Stars[0].Name);
    }
}
