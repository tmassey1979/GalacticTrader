namespace GalacticTrader.Services.Caching;

public interface ICacheInvalidationService
{
    Task InvalidatePlayerCacheAsync(Guid playerId);
    Task InvalidateSectorCacheAsync(Guid sectorId);
    Task InvalidateMarketCacheAsync(Guid sectorId);
    Task InvalidateLeaderboardCacheAsync(string leaderboardType);
    Task InvalidateRouteCacheAsync(Guid fromSectorId, Guid toSectorId);
    Task InvalidateCombatCacheAsync(Guid combatId);
}
