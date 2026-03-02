namespace GalacticTrader.Services.Caching;

using System.Collections.Concurrent;

public sealed class InMemoryCacheService : ICacheService
{
    private readonly ConcurrentDictionary<string, CacheEntry> _entries = new();

    public Task<T> GetAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult(default(T)!);
        }

        if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult(default(T)!);
        }

        return Task.FromResult((T)entry.Value);
    }

    public Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        DateTime? expiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null;
        _entries[key] = new CacheEntry(value!, expiresAt);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        _entries.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return Task.CompletedTask;
        }

        if (!pattern.Contains('*'))
        {
            _entries.TryRemove(pattern, out _);
            return Task.CompletedTask;
        }

        var prefix = pattern.Replace("*", string.Empty, StringComparison.Ordinal);
        var keysToRemove = _entries.Keys
            .Where(key => key.StartsWith(prefix, StringComparison.Ordinal))
            .ToList();

        foreach (var key in keysToRemove)
        {
            _entries.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }

    public Task FlushAsync()
    {
        _entries.Clear();
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentNullException(nameof(key));
        }

        if (!_entries.TryGetValue(key, out var entry))
        {
            return Task.FromResult(false);
        }

        if (entry.ExpiresAtUtc.HasValue && entry.ExpiresAtUtc.Value < DateTime.UtcNow)
        {
            _entries.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    private sealed record CacheEntry(object Value, DateTime? ExpiresAtUtc);
}
