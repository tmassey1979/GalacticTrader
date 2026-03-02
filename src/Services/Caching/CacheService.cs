using System;
using System.Text.Json;
using System.Threading.Tasks;
using StackExchange.Redis;

namespace GalacticTrader.Services.Caching
{
    /// <summary>
    /// Abstraction for Redis caching operations
    /// </summary>
    public interface ICacheService
    {
        Task<T> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveAsync(string key);
        Task RemoveByPatternAsync(string pattern);
        Task FlushAsync();
        Task<bool> ExistsAsync(string key);
    }

    /// <summary>
    /// Redis implementation of caching service
    /// </summary>
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly IServer _server;
        private readonly IConnectionMultiplexer _redis;

        public RedisCacheService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = redis.GetDatabase();
            _server = redis.GetServer(redis.GetEndPoints().First());
        }

        public async Task<T> GetAsync<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var value = await _db.StringGetAsync(key);
            
            if (!value.HasValue)
                return default;

            try
            {
                return JsonSerializer.Deserialize<T>(value.ToString());
            }
            catch (JsonException)
            {
                // Corrupted cache entry
                await RemoveAsync(key);
                return default;
            }
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            var json = JsonSerializer.Serialize(value);
            await _db.StringSetAsync(key, json, expiration);
        }

        public async Task RemoveAsync(string key)
        {
            if (string.IsNullOrEmpty(key))
                throw new ArgumentNullException(nameof(key));

            await _db.KeyDeleteAsync(key);
        }

        public async Task RemoveByPatternAsync(string pattern)
        {
            var keys = _server.Keys(pattern: pattern);
            var keysArray = keys.ToArray();
            
            if (keysArray.Length > 0)
            {
                await _db.KeyDeleteAsync(keysArray);
            }
        }

        public async Task FlushAsync()
        {
            await _server.FlushDatabaseAsync();
        }

        public async Task<bool> ExistsAsync(string key)
        {
            return await _db.KeyExistsAsync(key);
        }
    }
}
