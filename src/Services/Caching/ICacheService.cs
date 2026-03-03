namespace GalacticTrader.Services.Caching;

public interface ICacheService
{
    Task<T> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
    Task RemoveAsync(string key);
    Task RemoveByPatternAsync(string pattern);
    Task FlushAsync();
    Task<bool> ExistsAsync(string key);
}
