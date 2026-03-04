namespace GalacticTrader.API.Telemetry;

using Prometheus;

internal static class PrometheusMetrics
{
    public static readonly Histogram ApiRequestDuration = Metrics.CreateHistogram(
        "api_request_duration_seconds",
        "End-to-end API request latency in seconds.",
        new HistogramConfiguration
        {
            LabelNames = ["method", "route", "status_code"],
            Buckets = Histogram.ExponentialBuckets(0.005, 2, 12)
        });

    public static readonly Histogram CombatTickDuration = Metrics.CreateHistogram(
        "combat_tick_duration_seconds",
        "Combat tick execution time in seconds.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
        });

    public static readonly Histogram RouteCalculationDuration = Metrics.CreateHistogram(
        "route_calculation_time_seconds",
        "Route planning calculation time in seconds.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.0005, 2, 12)
        });

    public static readonly Histogram DbQueryDuration = Metrics.CreateHistogram(
        "db_query_duration_seconds",
        "Database-bound operation duration in seconds.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
        });

    public static readonly Gauge RedisCacheHitRatio = Metrics.CreateGauge(
        "redis_cache_hit_ratio",
        "Cache hit ratio (hits / total lookups).");

    public static readonly Gauge ActiveUsers = Metrics.CreateGauge(
        "active_users_current",
        "Currently active users.");

    public static readonly Gauge ActiveBattles = Metrics.CreateGauge(
        "active_battles_current",
        "Current active battles inferred from ships marked in combat.");

    public static readonly Gauge TotalCurrencyInCirculation = Metrics.CreateGauge(
        "total_currency_in_circulation",
        "Total currency currently tracked across players and faction treasuries.");

    public static readonly Counter StrategicIntelligenceExpiryRuns = Metrics.CreateCounter(
        "strategic_intelligence_expiry_runs_total",
        "Total number of scheduled intelligence expiry job runs.",
        new CounterConfiguration
        {
            LabelNames = ["status"]
        });

    public static readonly Counter StrategicIntelligenceReportsExpired = Metrics.CreateCounter(
        "strategic_intelligence_reports_expired_total",
        "Total number of intelligence reports expired by scheduled maintenance.");

    public static readonly Histogram StrategicIntelligenceExpiryDuration = Metrics.CreateHistogram(
        "strategic_intelligence_expiry_duration_seconds",
        "Duration of scheduled intelligence expiry job execution.",
        new HistogramConfiguration
        {
            Buckets = Histogram.ExponentialBuckets(0.001, 2, 12)
        });
}
