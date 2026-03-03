using GalacticTrader.MapGenerator.Api;
using GalacticTrader.MapGenerator.Generation;

namespace GalacticTrader.MapGenerator.Tests;

public sealed class MapSnapshotProjectorTests
{
    [Fact]
    public void BuildSectorRows_SortsByName_AndFormatsCoordinates()
    {
        var sectors = new[]
        {
            new SectorApiDto { Id = Guid.NewGuid(), Name = "Beta", X = 4.12f, Y = -2.05f, Z = 9.99f },
            new SectorApiDto { Id = Guid.NewGuid(), Name = "Alpha", X = 1f, Y = 2f, Z = 3f }
        };

        var rows = MapSnapshotProjector.BuildSectorRows(sectors);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alpha [1.0, 2.0, 3.0]", rows[0]);
        Assert.Equal("Beta [4.1, -2.0, 10.0]", rows[1]);
    }

    [Fact]
    public void BuildRouteRows_SortsByRiskDescending_AndFormats()
    {
        var routes = new[]
        {
            new RouteApiDto { FromSectorName = "Alpha", ToSectorName = "Gamma", BaseRiskScore = 44.24f },
            new RouteApiDto { FromSectorName = "Alpha", ToSectorName = "Beta", BaseRiskScore = 66.73f }
        };

        var rows = MapSnapshotProjector.BuildRouteRows(routes);

        Assert.Equal(2, rows.Count);
        Assert.Equal("Alpha -> Beta (Risk 66.7)", rows[0]);
        Assert.Equal("Alpha -> Gamma (Risk 44.2)", rows[1]);
    }
}
