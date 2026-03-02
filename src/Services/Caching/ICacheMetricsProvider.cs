namespace GalacticTrader.Services.Caching;

public interface ICacheMetricsProvider
{
    long CacheHits { get; }
    long CacheMisses { get; }
    double HitRatio { get; }
}
