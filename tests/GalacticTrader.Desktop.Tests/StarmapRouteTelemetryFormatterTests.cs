using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class StarmapRouteTelemetryFormatterTests
{
    [Fact]
    public void Build_FormatsRouteTelemetryLine()
    {
        var route = new RouteSegment(
            "A -> B",
            new Point3D(),
            new Point3D(1, 1, 1),
            IsHighRisk: true,
            BaseRiskScore: 67.5f,
            EconomicDensity: 53.1f,
            PiratePresenceProbability: 64.8f);

        var text = StarmapRouteTelemetryFormatter.Build(route);

        Assert.Equal("A -> B | Risk 67.5 | Density 53.1 | Pirate 64.8%", text);
    }
}
