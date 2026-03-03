using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Dashboard;
using GalacticTrader.Desktop.Starmap;
using System.Windows.Media.Media3D;

namespace GalacticTrader.Desktop.Tests;

public sealed class DashboardFleetProtectionLevelTests
{
    [Theory]
    [InlineData(0, "Unprotected")]
    [InlineData(12, "Fragile")]
    [InlineData(45, "Contested")]
    [InlineData(110, "Guarded")]
    [InlineData(180, "Fortified")]
    public void Build_MapsFleetStrengthToProtectionLevel(int fleetStrength, string expectedLevel)
    {
        var summary = DashboardSummaryBuilder.Build(
            transactions: [],
            ships: [],
            escortSummary: new EscortSummaryApiDto { FleetStrength = fleetStrength },
            standings: [],
            dangerousRoutes: [],
            reports: [],
            scene: new StarmapScene([], [], new Model3DGroup()));

        Assert.Equal(expectedLevel, summary.FleetProtectionLevel);
    }
}
