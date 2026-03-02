using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GalacticTrader.Services.Caching
{
    /// <summary>
    /// Cache invalidation strategy to manage cache coherence
    /// </summary>
    public interface ICacheInvalidationService
    {
        Task InvalidatePlayerCacheAsync(Guid playerId);
        Task InvalidateSectorCacheAsync(Guid sectorId);
        Task InvalidateMarketCacheAsync(Guid sectorId);
        Task InvalidateLeaderboardCacheAsync(string leaderboardType);
        Task InvalidateRouteCacheAsync(Guid fromSectorId, Guid toSectorId);
        Task InvalidateCombatCacheAsync(Guid combatId);
    }

    public class CacheInvalidationService : ICacheInvalidationService
    {
        private readonly ICacheService _cache;

        public CacheInvalidationService(ICacheService cache)
        {
            _cache = cache;
        }

        /// <summary>
        /// Invalidate all cache entries related to a player
        /// </summary>
        public async Task InvalidatePlayerCacheAsync(Guid playerId)
        {
            var playerSessionKey = string.Format(CacheKeys.PLAYER_SESSION, playerId);
            await _cache.RemoveAsync(playerSessionKey);
        }

        /// <summary>
        /// Invalidate all cache entries for a sector
        /// </summary>
        public async Task InvalidateSectorCacheAsync(Guid sectorId)
        {
            var sectorKey = string.Format(CacheKeys.SECTOR_DATA, sectorId);
            var routesKey = string.Format(CacheKeys.SECTOR_ROUTES, sectorId);
            
            await Task.WhenAll(
                _cache.RemoveAsync(sectorKey),
                _cache.RemoveAsync(routesKey),
                _cache.RemoveAsync(CacheKeys.SECTOR_GRAPH) // Entire graph affected
            );
        }

        /// <summary>
        /// Invalidate market cache for a sector
        /// </summary>
        public async Task InvalidateMarketCacheAsync(Guid sectorId)
        {
            var marketKey = string.Format(CacheKeys.SECTOR_MARKET, sectorId);
            await _cache.RemoveAsync(marketKey);
            
            // Also invalidate market heatmap
            await _cache.RemoveAsync(CacheKeys.MARKET_HEATMAP);
        }

        /// <summary>
        /// Invalidate specific leaderboard
        /// </summary>
        public async Task InvalidateLeaderboardCacheAsync(string leaderboardType)
        {
            var key = leaderboardType switch
            {
                "wealth" => CacheKeys.LEADERBOARD_WEALTH,
                "reputation" => CacheKeys.LEADERBOARD_REPUTATION,
                "combat" => CacheKeys.LEADERBOARD_COMBAT,
                "trade" => CacheKeys.LEADERBOARD_TRADE,
                _ => null
            };

            if (key != null)
            {
                await _cache.RemoveAsync(key);
            }
        }

        /// <summary>
        /// Invalidate route cache
        /// </summary>
        public async Task InvalidateRouteCacheAsync(Guid fromSectorId, Guid toSectorId)
        {
            var routeKey = string.Format(CacheKeys.ROUTE_DETAILS, fromSectorId, toSectorId);
            var reverseRouteKey = string.Format(CacheKeys.ROUTE_DETAILS, toSectorId, fromSectorId);
            
            await Task.WhenAll(
                _cache.RemoveAsync(routeKey),
                _cache.RemoveAsync(reverseRouteKey),
                _cache.RemoveAsync(CacheKeys.SECTOR_GRAPH) // Entire graph affected
            );
        }

        /// <summary>
        /// Invalidate combat cache
        /// </summary>
        public async Task InvalidateCombatCacheAsync(Guid combatId)
        {
            var combatKey = string.Format(CacheKeys.COMBAT_STATE, combatId);
            await _cache.RemoveAsync(combatKey);
        }
    }
}
