using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Routes;

namespace GalacticTrader.Desktop.Tests;

public sealed class RoutePlanAssemblerTests
{
    [Fact]
    public void Combine_MergesLegTotalsAndPathWithoutDuplicateBoundaryNodes()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();
        var d = Guid.NewGuid();

        var first = BuildPlan(
            from: a,
            to: c,
            cost: 100,
            fuel: 10,
            risk: 4,
            travelSeconds: 120,
            sectorPath: [a, b, c],
            hops: [BuildHop(a, b), BuildHop(b, c)]);
        var second = BuildPlan(
            from: c,
            to: d,
            cost: 55,
            fuel: 6,
            risk: 2,
            travelSeconds: 70,
            sectorPath: [c, d],
            hops: [BuildHop(c, d)]);

        var merged = RoutePlanAssembler.Combine([first, second]);

        Assert.Equal(a, merged.FromSectorId);
        Assert.Equal(d, merged.ToSectorId);
        Assert.Equal(155d, merged.TotalCost);
        Assert.Equal(16d, merged.TotalFuelCost);
        Assert.Equal(6d, merged.TotalRiskScore);
        Assert.Equal(190, merged.TotalTravelTimeSeconds);
        Assert.Equal(new[] { a, b, c, d }, merged.SectorPath);
        Assert.Equal(3, merged.Hops.Count);
    }

    [Fact]
    public void CombineOptimization_MergesProfilesAcrossSegments()
    {
        var a = Guid.NewGuid();
        var b = Guid.NewGuid();
        var c = Guid.NewGuid();

        var segmentOne = new RouteOptimizationApiDto
        {
            Fastest = BuildPlan(a, b, 30, 3, 1, 40, [a, b], [BuildHop(a, b)]),
            Cheapest = BuildPlan(a, b, 20, 2, 3, 60, [a, b], [BuildHop(a, b)]),
            Safest = BuildPlan(a, b, 25, 2.5, 0.5, 70, [a, b], [BuildHop(a, b)]),
            Balanced = BuildPlan(a, b, 24, 2.4, 1.5, 55, [a, b], [BuildHop(a, b)])
        };
        var segmentTwo = new RouteOptimizationApiDto
        {
            Fastest = BuildPlan(b, c, 40, 4, 2, 50, [b, c], [BuildHop(b, c)]),
            Cheapest = BuildPlan(b, c, 35, 3.5, 4, 75, [b, c], [BuildHop(b, c)]),
            Safest = BuildPlan(b, c, 45, 4.5, 0.8, 80, [b, c], [BuildHop(b, c)]),
            Balanced = BuildPlan(b, c, 38, 3.8, 2.2, 65, [b, c], [BuildHop(b, c)])
        };

        var merged = RouteOptimizationAssembler.Combine([segmentOne, segmentTwo]);

        Assert.NotNull(merged.Fastest);
        Assert.Equal(70d, merged.Fastest!.TotalCost);
        Assert.Equal(new[] { a, b, c }, merged.Fastest.SectorPath);
        Assert.NotNull(merged.Cheapest);
        Assert.Equal(55d, merged.Cheapest!.TotalCost);
        Assert.NotNull(merged.Safest);
        Assert.Equal(1.3d, merged.Safest!.TotalRiskScore);
        Assert.NotNull(merged.Balanced);
        Assert.Equal(3.7d, merged.Balanced!.TotalRiskScore);
    }

    private static RoutePlanApiDto BuildPlan(
        Guid from,
        Guid to,
        double cost,
        double fuel,
        double risk,
        int travelSeconds,
        IReadOnlyList<Guid> sectorPath,
        IReadOnlyList<RouteHopApiDto> hops)
    {
        return new RoutePlanApiDto
        {
            FromSectorId = from,
            ToSectorId = to,
            Algorithm = "dijkstra",
            TravelMode = 0,
            TotalCost = cost,
            TotalFuelCost = fuel,
            TotalRiskScore = risk,
            TotalTravelTimeSeconds = travelSeconds,
            SectorPath = sectorPath,
            Hops = hops
        };
    }

    private static RouteHopApiDto BuildHop(Guid from, Guid to)
    {
        return new RouteHopApiDto
        {
            FromSectorId = from,
            ToSectorId = to
        };
    }
}
