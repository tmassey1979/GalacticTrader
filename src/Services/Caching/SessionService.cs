using System;
using System.Threading.Tasks;

namespace GalacticTrader.Services.Caching
{
    /// <summary>
    /// Session-related cache constants and conventions
    /// </summary>
    public static class CacheKeys
    {
        // Session Keys
        public const string SESSION_PREFIX = "session:";
        public const string PLAYER_SESSION = "session:player:{0}";
        public const string ACTIVE_SESSIONS = "sessions:active";
        
        // Route Cache Keys
        public const string ROUTE_CACHE_PREFIX = "route:";
        public const string SECTOR_ROUTES = "routes:sector:{0}";
        public const string ROUTE_DETAILS = "route:{0}:{1}"; // From:To
        
        // Leaderboard Cache Keys
        public const string LEADERBOARD_PREFIX = "leaderboard:";
        public const string LEADERBOARD_WEALTH = "leaderboard:wealth";
        public const string LEADERBOARD_REPUTATION = "leaderboard:reputation";
        public const string LEADERBOARD_COMBAT = "leaderboard:combat";
        public const string LEADERBOARD_TRADE = "leaderboard:trade";
        
        // Market Cache Keys
        public const string MARKET_PREFIX = "market:";
        public const string SECTOR_MARKET = "market:sector:{0}";
        public const string COMMODITY_PRICE = "price:commodity:{0}:sector:{1}";
        public const string MARKET_HEATMAP = "market:heatmap";
        
        // Sector Cache Keys
        public const string SECTOR_PREFIX = "sector:";
        public const string SECTOR_DATA = "sector:{0}";
        public const string SECTOR_GRAPH = "sector:graph";
        
        // Combat Cache Keys
        public const string COMBAT_PREFIX = "combat:";
        public const string ACTIVE_BATTLES = "combat:active";
        public const string COMBAT_STATE = "combat:state:{0}";
        
        // NPC Cache Keys
        public const string NPC_PREFIX = "npc:";
        public const string NPC_AGENT = "npc:agent:{0}";
        public const string ACTIVE_NPCS = "npc:active";
    }

    /// <summary>
    /// Player session information
    /// </summary>
    public class PlayerSession
    {
        public Guid PlayerId { get; set; }
        public string Username { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime LastActivityTime { get; set; }
        public string IpAddress { get; set; }
        public string UserAgent { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Session management service
    /// </summary>
    public interface ISessionService
    {
        Task<PlayerSession> CreateSessionAsync(Guid playerId, string username, string ipAddress, string userAgent);
        Task<PlayerSession> GetSessionAsync(Guid playerId);
        Task UpdateSessionActivityAsync(Guid playerId);
        Task InvalidateSessionAsync(Guid playerId);
        Task<int> GetActiveSessions();
    }

    public class SessionService : ISessionService
    {
        private readonly ICacheService _cache;
        private readonly TimeSpan _sessionExpiration = TimeSpan.FromHours(24);

        public SessionService(ICacheService cache)
        {
            _cache = cache;
        }

        public async Task<PlayerSession> CreateSessionAsync(Guid playerId, string username, string ipAddress, string userAgent)
        {
            var session = new PlayerSession
            {
                PlayerId = playerId,
                Username = username,
                LoginTime = DateTime.UtcNow,
                LastActivityTime = DateTime.UtcNow,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                IsActive = true
            };

            var key = string.Format(CacheKeys.PLAYER_SESSION, playerId);
            await _cache.SetAsync(key, session, _sessionExpiration);
            
            return session;
        }

        public async Task<PlayerSession> GetSessionAsync(Guid playerId)
        {
            var key = string.Format(CacheKeys.PLAYER_SESSION, playerId);
            var session = await _cache.GetAsync<PlayerSession>(key);
            
            if (session != null && session.IsActive)
            {
                await UpdateSessionActivityAsync(playerId);
            }
            
            return session;
        }

        public async Task UpdateSessionActivityAsync(Guid playerId)
        {
            var session = await GetSessionAsync(playerId);
            if (session != null)
            {
                session.LastActivityTime = DateTime.UtcNow;
                var key = string.Format(CacheKeys.PLAYER_SESSION, playerId);
                await _cache.SetAsync(key, session, _sessionExpiration);
            }
        }

        public async Task InvalidateSessionAsync(Guid playerId)
        {
            var key = string.Format(CacheKeys.PLAYER_SESSION, playerId);
            await _cache.RemoveAsync(key);
        }

        public async Task<int> GetActiveSessions()
        {
            var sessions = await _cache.GetAsync<Dictionary<Guid, PlayerSession>>(CacheKeys.ACTIVE_SESSIONS);
            return sessions?.Count ?? 0;
        }
    }
}
