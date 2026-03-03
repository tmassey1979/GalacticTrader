namespace GalacticTrader.Services.Caching;

internal sealed record InMemoryCacheEntry(object Value, DateTime? ExpiresAtUtc);
