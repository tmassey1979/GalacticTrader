using GalacticTrader.MapGenerator.Generation;

namespace GalacticTrader.MapGenerator.Tests;

public sealed class MapLayoutGeneratorTests
{
    [Fact]
    public void Generate_ProducesExpectedSectorCountAndAtLeastSpanningRoutes()
    {
        var generator = new MapLayoutGenerator();

        var layout = generator.Generate(seed: 1234, sectorCount: 20, routeDensity: 2);

        Assert.Equal(20, layout.Sectors.Count);
        Assert.True(layout.Routes.Count >= 20);
    }

    [Fact]
    public void Generate_DoesNotCreateSelfRoutesOrDuplicateUndirectedEdges()
    {
        var generator = new MapLayoutGenerator();
        var layout = generator.Generate(seed: 9876, sectorCount: 24, routeDensity: 3);

        var edges = new HashSet<(int A, int B)>();
        foreach (var route in layout.Routes)
        {
            Assert.NotEqual(route.FromIndex, route.ToIndex);

            var edge = route.FromIndex < route.ToIndex
                ? (route.FromIndex, route.ToIndex)
                : (route.ToIndex, route.FromIndex);
            Assert.True(edges.Add(edge));
        }
    }
}
