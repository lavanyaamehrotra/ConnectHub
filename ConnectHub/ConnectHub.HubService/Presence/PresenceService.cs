using StackExchange.Redis;
using System.Text.Json;
using ConnectHub.HubService.Models;

namespace ConnectHub.HubService.Presence
{
    // ============================================================
    // UC4 — Redis-backed PresenceService
    //
    // WHY REDIS?
    // The old ConcurrentDictionary lives in one process.
    // If you run 2 HubService pods, each has its own dictionary =
    // "User A is online" on pod-1 but "offline" on pod-2. Broken.
    // Redis is a shared external store — all pods see the same data.
    //
    // REDIS DATA STRUCTURES USED:
    //
    // 1. SET  key: "presence:user:{userId}"
    //    value: { connectionId1, connectionId2, ... }
    //    → "Which connections does this user have?"
    //    → TTL: 24h (auto-expires stale entries after server crash)
    //
    // 2. HASH key: "presence:connections"
    //    field: connectionId   value: JSON of UserConnection
    //    → "Who owns this connectionId?"
    //    → Used by GetOnlineUsersInfoAsync()
    //
    // 3. SET  key: "presence:online_users"
    //    value: { userId1, userId2, ... }
    //    → "Who is currently online?"
    //    → Used by GetOnlineUserIdsAsync()
    // ============================================================
    public class PresenceService : IPresenceService
    {
        private readonly IDatabase _db;
        private readonly ILogger<PresenceService> _logger;

        // Key helpers — keeps key names consistent
        private static string UserKey(Guid userId)      => $"presence:user:{userId}";
        private const  string ConnectionsHash           =  "presence:connections";
        private const  string OnlineUsersSet            =  "presence:online_users";
        private static readonly TimeSpan UserKeyTtl     = TimeSpan.FromHours(24);

        public PresenceService(IConnectionMultiplexer redis, ILogger<PresenceService> logger)
        {
            _db     = redis.GetDatabase();
            _logger = logger;
        }

        public async Task PurgeStalePresenceAsync()
        {
            try
            {
                await _db.KeyDeleteAsync(ConnectionsHash);
                await _db.KeyDeleteAsync(OnlineUsersSet);
                _logger.LogInformation("Cleared stale presence data on startup.");
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not purge presence data: {Msg}", ex.Message);
            }
        }

        // -----------------------------------------------------------
        // Called from ChatHub.OnConnectedAsync()
        // 1. Add connectionId to the user's Redis SET
        // 2. Store UserConnection metadata in the connections HASH
        // 3. Add userId to the online-users SET
        // -----------------------------------------------------------
        public async Task UserConnectedAsync(Guid userId, string connectionId)
        {
            try
            {
                // Add this connectionId to the user's set
                await _db.SetAddAsync(UserKey(userId), connectionId);

                // Refresh TTL so active users never expire mid-session
                await _db.KeyExpireAsync(UserKey(userId), UserKeyTtl);

                // Store connection metadata (for GetOnlineUsersInfoAsync)
                var info = new UserConnection
                {
                    ConnectionId = connectionId,
                    UserId       = userId,
                    ConnectedAt  = DateTime.UtcNow
                };
                await _db.HashSetAsync(ConnectionsHash, connectionId, JsonSerializer.Serialize(info));

                // Track this userId as online
                await _db.SetAddAsync(OnlineUsersSet, userId.ToString());

                _logger.LogInformation("User {UserId} connected via Redis. ConnId: {ConnId}", userId, connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis UserConnectedAsync failed: {Msg}", ex.Message);
            }
        }

        public async Task UserDisconnectedAsync(Guid userId, string connectionId)
        {
            try
            {
                await _db.SetRemoveAsync(UserKey(userId), connectionId);
                await _db.HashDeleteAsync(ConnectionsHash, connectionId);

                // Check if user has any remaining connections
                var remaining = await _db.SetLengthAsync(UserKey(userId));
                if (remaining == 0)
                {
                    await _db.KeyDeleteAsync(UserKey(userId));
                    await _db.SetRemoveAsync(OnlineUsersSet, userId.ToString());
                    _logger.LogInformation("User {UserId} is now OFFLINE (Redis)", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis UserDisconnectedAsync failed: {Msg}", ex.Message);
            }
        }

        public async Task<List<string>> GetConnectionsByUserIdAsync(Guid userId)
        {
            try
            {
                var members = await _db.SetMembersAsync(UserKey(userId));
                return members.Select(m => m.ToString()).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GetConnectionsByUserIdAsync failed: {Msg}", ex.Message);
                return new List<string>();
            }
        }

        public async Task<List<Guid>> GetOnlineUserIdsAsync()
        {
            try
            {
                var members = await _db.SetMembersAsync(OnlineUsersSet);
                var result = new List<Guid>();
                foreach (var m in members)
                {
                    if (Guid.TryParse(m.ToString(), out var userId))
                    {
                        if (await _db.KeyExistsAsync(UserKey(userId)))
                        {
                            result.Add(userId);
                        }
                        else
                        {
                            await _db.SetRemoveAsync(OnlineUsersSet, m);
                        }
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GetOnlineUserIdsAsync failed: {Msg}", ex.Message);
                return new List<Guid>();
            }
        }

        public async Task<bool> IsUserOnlineAsync(Guid userId)
        {
            try
            {
                var inSet = await _db.SetContainsAsync(OnlineUsersSet, userId.ToString());
                if (!inSet) return false;
                
                return await _db.KeyExistsAsync(UserKey(userId));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis IsUserOnlineAsync failed: {Msg}", ex.Message);
                return false;
            }
        }

        public async Task<int> GetConnectionCountAsync()
        {
            try
            {
                var onlineUsers = await GetOnlineUserIdsAsync();
                var total = 0;
                foreach (var uid in onlineUsers)
                    total += (int)await _db.SetLengthAsync(UserKey(uid));
                return total;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GetConnectionCountAsync failed: {Msg}", ex.Message);
                return 0;
            }
        }

        public async Task<List<UserConnection>> GetOnlineUsersInfoAsync()
        {
            try
            {
                var entries = await _db.HashGetAllAsync(ConnectionsHash);
                var result  = new List<UserConnection>();
                foreach (var entry in entries)
                {
                    if (!entry.Value.IsNullOrEmpty)
                    {
                        var conn = JsonSerializer.Deserialize<UserConnection>(entry.Value.ToString());
                        if (conn != null) result.Add(conn);
                    }
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GetOnlineUsersInfoAsync failed: {Msg}", ex.Message);
                return new List<UserConnection>();
            }
        }

        public async Task ClearUserConnectionsAsync(Guid userId)
        {
            try
            {
                var connections = await GetConnectionsByUserIdAsync(userId);
                foreach (var connId in connections)
                    await _db.HashDeleteAsync(ConnectionsHash, connId);

                await _db.KeyDeleteAsync(UserKey(userId));
                await _db.SetRemoveAsync(OnlineUsersSet, userId.ToString());

                _logger.LogInformation("Cleared all Redis connections for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis ClearUserConnectionsAsync failed: {Msg}", ex.Message);
            }
        }
    }
}