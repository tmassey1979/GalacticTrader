using GalacticTrader.Services.Combat;
using GalacticTrader.Services.Fleet;
using GalacticTrader.Services.Market;
using GalacticTrader.Services.Navigation;
using GalacticTrader.Services.Realtime;
using GalacticTrader.Services.Reputation;
using GalacticTrader.Services.Strategic;

namespace GalacticTrader.Tests;

public sealed class DashboardRealtimeSnapshotProjectionTests
{
    [Fact]
    public void Build_ProjectsMetricsAndEvents()
    {
        var capturedAtUtc = new DateTime(2026, 3, 3, 2, 0, 0, DateTimeKind.Utc);
        var transactions = new[]
        {
            new TradeExecutionResult
            {
                ActionType = TradeActionType.Buy,
                Quantity = 12,
                TariffAmount = 18m,
                TotalPrice = 300m,
                RemainingPlayerCredits = 900m,
                Status = "success"
            }
        };
        var standings = new[]
        {
            new PlayerFactionStandingDto { ReputationScore = 10 },
            new PlayerFactionStandingDto { ReputationScore = 27 }
        };
        var escortSummary = new EscortSummaryDto
        {
            FleetStrength = 45
        };
        var routes = new[]
        {
            new RouteDto { Id = Guid.NewGuid(), BaseRiskScore = 30f },
            new RouteDto { Id = Guid.NewGuid(), BaseRiskScore = 80f }
        };
        var dangerousRoutes = new[]
        {
            new RouteDto { Id = Guid.NewGuid(), BaseRiskScore = 78f },
            new RouteDto { Id = Guid.NewGuid(), BaseRiskScore = 83f },
            new RouteDto { Id = Guid.NewGuid(), BaseRiskScore = 91f }
        };
        var reports = new[]
        {
            new IntelligenceReportDto
            {
                SignalType = "Pirates",
                SectorName = "Orion",
                Payload = "Intercepted chatter",
                ConfidenceScore = 0.72f,
                DetectedAt = capturedAtUtc.AddMinutes(-5),
                IsExpired = false
            },
            new IntelligenceReportDto
            {
                SignalType = "Noise",
                SectorName = "Draco",
                Payload = "stale signal",
                ConfidenceScore = 0.25f,
                DetectedAt = capturedAtUtc.AddMinutes(-45),
                IsExpired = true
            }
        };
        var combatLogs = new[]
        {
            new CombatLogDto
            {
                BattleOutcome = "Victory",
                BattleEndedAt = capturedAtUtc.AddMinutes(-2),
                DurationSeconds = 33,
                TotalTicks = 7,
                InsurancePayout = 21m
            }
        };

        var snapshot = DashboardRealtimeSnapshotProjection.Build(
            transactions,
            standings,
            escortSummary,
            routes,
            dangerousRoutes,
            reports,
            combatLogs,
            capturedAtUtc);

        Assert.Equal(900m, snapshot.Metrics.LiquidCredits);
        Assert.Equal(27, snapshot.Metrics.ReputationScore);
        Assert.Equal(45, snapshot.Metrics.FleetStrength);
        Assert.Equal(2, snapshot.Metrics.ActiveRoutes);
        Assert.Equal(4, snapshot.Metrics.AlertCount);

        Assert.Equal(3, snapshot.Events.Count);
        Assert.Equal("Trade", snapshot.Events[0].Category);
        Assert.Equal("Combat", snapshot.Events[1].Category);
        Assert.Equal("Intel", snapshot.Events[2].Category);
    }
}
