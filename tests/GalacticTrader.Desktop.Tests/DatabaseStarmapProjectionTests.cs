using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Starmap;

namespace GalacticTrader.Desktop.Tests;

public sealed class DatabaseStarmapProjectionTests
{
    [Fact]
    public void ToStars_ProjectsSectorCoordinatesIntoStarNodes()
    {
        var sectors = new List<SectorApiDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Alpha", X = 10, Y = 20, Z = 30 },
            new() { Id = Guid.NewGuid(), Name = "Beta", X = -4, Y = 3, Z = 88 }
        };

        var stars = DatabaseStarmapProjection.ToStars(sectors);

        Assert.Equal(2, stars.Count);
        Assert.Equal("Alpha", stars[0].Name);
        Assert.Equal(10, stars[0].Position.X);
        Assert.Equal(88, stars[1].Position.Z);
    }

    [Fact]
    public void ToRoutes_ProjectsOnlyRoutesWithKnownSectors()
    {
        var sectorA = new SectorApiDto { Id = Guid.NewGuid(), Name = "A", X = 0, Y = 0, Z = 0 };
        var sectorB = new SectorApiDto { Id = Guid.NewGuid(), Name = "B", X = 10, Y = 0, Z = 0 };
        var sectors = new List<SectorApiDto> { sectorA, sectorB };
        var routes = new List<RouteApiDto>
        {
            new() { Id = Guid.NewGuid(), FromSectorId = sectorA.Id, ToSectorId = sectorB.Id, BaseRiskScore = 75 },
            new() { Id = Guid.NewGuid(), FromSectorId = sectorA.Id, ToSectorId = Guid.NewGuid(), BaseRiskScore = 10 }
        };

        var projected = DatabaseStarmapProjection.ToRoutes(routes, sectors);

        Assert.Single(projected);
        Assert.Equal("A -> B", projected[0].Name);
        Assert.True(projected[0].IsHighRisk);
    }
}
