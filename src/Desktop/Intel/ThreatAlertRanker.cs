using GalacticTrader.Desktop.Api;

namespace GalacticTrader.Desktop.Intel;

public static class ThreatAlertRanker
{
    public static IReadOnlyList<ThreatAlert> Build(
        IReadOnlyList<RouteApiDto> dangerousRoutes,
        IReadOnlyList<IntelligenceReportApiDto> intelligenceReports,
        int maxItems = 12)
    {
        var routeAlerts = dangerousRoutes.Select(route => new ThreatAlert
        {
            Source = "Route",
            Headline = $"{route.FromSectorName} -> {route.ToSectorName}",
            Detail = $"Base risk {route.BaseRiskScore:N1}",
            Severity = route.BaseRiskScore
        });

        var reportAlerts = intelligenceReports
            .Where(static report => !report.IsExpired)
            .Select(report => new ThreatAlert
            {
                Source = "Intel",
                Headline = $"{report.SignalType} in {report.SectorName}",
                Detail = report.Payload,
                Severity = Math.Clamp(report.ConfidenceScore * 100f, 0f, 100f)
            });

        return routeAlerts
            .Concat(reportAlerts)
            .OrderByDescending(static alert => alert.Severity)
            .ThenBy(static alert => alert.Source)
            .Take(Math.Max(1, maxItems))
            .ToArray();
    }
}
