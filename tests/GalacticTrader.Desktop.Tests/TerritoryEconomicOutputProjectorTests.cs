using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class TerritoryEconomicOutputProjectorTests
{
    [Fact]
    public void BuildPerSystem_AccountsForDominanceAndPolicyModifiers()
    {
        var record = new TerritoryDominanceApiDto
        {
            ControlledSectorCount = 4,
            InfrastructureControlScore = 68f,
            DominanceScore = 54f,
            WarMomentumScore = 20f
        };

        var output = TerritoryEconomicOutputProjector.BuildPerSystem(record, taxRatePercent: 11m, tradeIncentivePercent: 4m);

        Assert.Equal(1624.63m, output);
    }

    [Fact]
    public void BuildPerSystem_ReturnsZero_WhenNoControlledSectors()
    {
        var output = TerritoryEconomicOutputProjector.BuildPerSystem(
            new TerritoryDominanceApiDto { ControlledSectorCount = 0 },
            taxRatePercent: 5m,
            tradeIncentivePercent: 2m);

        Assert.Equal(0m, output);
    }
}
