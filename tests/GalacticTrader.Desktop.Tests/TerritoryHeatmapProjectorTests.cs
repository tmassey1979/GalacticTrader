using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class TerritoryHeatmapProjectorTests
{
    [Fact]
    public void Build_SortsByDominanceAndAppliesPriority()
    {
        var alphaId = Guid.NewGuid();
        var betaId = Guid.NewGuid();
        var records = new[]
        {
            new TerritoryDominanceApiDto { FactionId = alphaId, FactionName = "Alpha", DominanceScore = 42f },
            new TerritoryDominanceApiDto
            {
                FactionId = betaId,
                FactionName = "Beta",
                DominanceScore = 78f,
                ControlledSectorCount = 6,
                InfrastructureControlScore = 62f,
                WarMomentumScore = 18f
            }
        };

        var rows = TerritoryHeatmapProjector.Build(
            records,
            new Dictionary<Guid, string>
            {
                [betaId] = "High"
            },
            new Dictionary<Guid, TerritoryEconomicPolicyApiDto>
            {
                [betaId] = new TerritoryEconomicPolicyApiDto
                {
                    FactionId = betaId,
                    FactionName = "Beta",
                    TaxRate = 0.11m,
                    TradeIncentiveModifier = 0.04m
                }
            });

        Assert.Equal(2, rows.Count);
        Assert.Equal("Beta", rows[0].FactionName);
        Assert.Equal("High", rows[0].ProtectionPriority);
        Assert.True(rows[0].EconomicOutputPerSystem > 0m);
        Assert.Equal(11m, rows[0].TaxRatePercent);
        Assert.Equal(4m, rows[0].TradeIncentivePercent);
    }

    [Fact]
    public void Build_AssignsExpectedHeatBands()
    {
        var records = new[]
        {
            new TerritoryDominanceApiDto { FactionId = Guid.NewGuid(), FactionName = "Low", DominanceScore = 10f },
            new TerritoryDominanceApiDto { FactionId = Guid.NewGuid(), FactionName = "Critical", DominanceScore = 95f }
        };

        var rows = TerritoryHeatmapProjector.Build(records, new Dictionary<Guid, string>(), new Dictionary<Guid, TerritoryEconomicPolicyApiDto>());
        var low = rows.Single(row => row.FactionName == "Low");
        var critical = rows.Single(row => row.FactionName == "Critical");

        Assert.Equal("#2E7D32", low.HeatHex);
        Assert.Equal("#C62828", critical.HeatHex);
    }
}
