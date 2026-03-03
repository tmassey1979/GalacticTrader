using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RouteWaypointParserTests
{
    [Fact]
    public void TryParse_ResolvesCommaSeparatedWaypointNames()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var gamma = Guid.NewGuid();
        var delta = Guid.NewGuid();
        var sectors = new[]
        {
            new RouteSectorSelectionItem { SectorId = alpha, Name = "Alpha" },
            new RouteSectorSelectionItem { SectorId = beta, Name = "Beta" },
            new RouteSectorSelectionItem { SectorId = gamma, Name = "Gamma" },
            new RouteSectorSelectionItem { SectorId = delta, Name = "Delta" }
        };

        var success = RouteWaypointParser.TryParse(
            " Beta, gamma ",
            sectors,
            fromSectorId: alpha,
            toSectorId: delta,
            out var waypoints,
            out var error);

        Assert.True(success);
        Assert.Null(error);
        Assert.Equal(new[] { beta, gamma }, waypoints);
    }

    [Fact]
    public void TryParse_RejectsUnknownWaypoint()
    {
        var alpha = Guid.NewGuid();
        var delta = Guid.NewGuid();
        var sectors = new[]
        {
            new RouteSectorSelectionItem { SectorId = alpha, Name = "Alpha" },
            new RouteSectorSelectionItem { SectorId = delta, Name = "Delta" }
        };

        var success = RouteWaypointParser.TryParse(
            "Unknown",
            sectors,
            fromSectorId: alpha,
            toSectorId: delta,
            out _,
            out var error);

        Assert.False(success);
        Assert.Contains("does not match", error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void TryParse_RejectsOriginOrDestinationAndDuplicates()
    {
        var alpha = Guid.NewGuid();
        var beta = Guid.NewGuid();
        var delta = Guid.NewGuid();
        var sectors = new[]
        {
            new RouteSectorSelectionItem { SectorId = alpha, Name = "Alpha" },
            new RouteSectorSelectionItem { SectorId = beta, Name = "Beta" },
            new RouteSectorSelectionItem { SectorId = delta, Name = "Delta" }
        };

        var originFailure = RouteWaypointParser.TryParse(
            "Alpha",
            sectors,
            fromSectorId: alpha,
            toSectorId: delta,
            out _,
            out var originError);

        Assert.False(originFailure);
        Assert.Contains("origin or destination", originError, StringComparison.OrdinalIgnoreCase);

        var duplicateFailure = RouteWaypointParser.TryParse(
            "Beta, Beta",
            sectors,
            fromSectorId: alpha,
            toSectorId: delta,
            out _,
            out var duplicateError);

        Assert.False(duplicateFailure);
        Assert.Contains("duplicated", duplicateError, StringComparison.OrdinalIgnoreCase);
    }
}
