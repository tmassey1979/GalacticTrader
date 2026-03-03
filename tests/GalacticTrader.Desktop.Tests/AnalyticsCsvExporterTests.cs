using GalacticTrader.Desktop.Api;
using GalacticTrader.Desktop.Modules;

namespace GalacticTrader.Desktop.Tests;

public sealed class AnalyticsCsvExporterTests
{
    [Fact]
    public void BuildCsv_ContainsSectionsForTradesAndCombats()
    {
        var trades = new[]
        {
            new TradeExecutionResultApiDto
            {
                TradeTransactionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                Quantity = 3,
                UnitPrice = 10m,
                TariffAmount = 1m,
                TotalPrice = 31m
            }
        };

        var combats = new[]
        {
            new CombatLogApiDto
            {
                Id = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                BattleEndedAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc),
                DurationSeconds = 20,
                TotalTicks = 2,
                InsurancePayout = 5m,
                BattleOutcome = "Victory"
            }
        };

        var csv = AnalyticsCsvExporter.BuildCsv(trades, combats);

        Assert.Contains("section,id,timestamp", csv, StringComparison.Ordinal);
        Assert.Contains("trade,aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa", csv, StringComparison.Ordinal);
        Assert.Contains("combat,bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb", csv, StringComparison.Ordinal);
    }
}
