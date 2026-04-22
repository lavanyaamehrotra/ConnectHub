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

        // -----------------------------------------------------------
        // Called from ChatHub.OnConnectedAsync()
        // 1. Add connectionId to the user's Redis SET
        // 2. Store UserConnection metadata in the connections HASH
        // 3. Add userId to the online-users SET
        // -----------------------------------------------------------
        public async Task UserConnectedAsync(Guid userId, string connectionId)
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

        // -----------------------------------------------------------
        // Called from ChatHub.OnDisconnectedAsync()
        // 1. Remove connectionId from user's SET
        // 2. If SET is now empty → user is offline, remove from online SET
        // 3. Remove connection metadata from HASH
        // -----------------------------------------------------------
        public async Task UserDisconnectedAsync(Guid userId, string connectionId)
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

        // Returns all connectionIds for a user (multi-device support)
        public async Task<List<string>> GetConnectionsByUserIdAsync(Guid userId)
        {
            var members = await _db.SetMembersAsync(UserKey(userId));
            return members.Select(m => m.ToString()).ToList();
        }

        // Returns all currently online user IDs
        public async Task<List<Guid>> GetOnlineUserIdsAsync()
        {
            var members = await _db.SetMembersAsync(OnlineUsersSet);
            return members
                .Select(m => Guid.TryParse(m.ToString(), out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();
        }

        // True if user has at least one active connection
        public async Task<bool> IsUserOnlineAsync(Guid userId)
        {
            return await _db.SetContainsAsync(OnlineUsersSet, userId.ToString());
        }

        // Total active connections across all users
        public async Task<int> GetConnectionCountAsync()
        {
            var onlineUsers = await GetOnlineUserIdsAsync();
            var total = 0;
            foreach (var uid in onlineUsers)
                total += (int)await _db.SetLengthAsync(UserKey(uid));
            return total;
        }

        // Detailed info about all active connections (for admin)
        public async Task<List<UserConnection>> GetOnlineUsersInfoAsync()
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

        // Force-remove all connections for a user (admin suspend)
        public async Task ClearUserConnectionsAsync(Guid userId)
        {
            var connections = await GetConnectionsByUserIdAsync(userId);
            foreach (var connId in connections)
                await _db.HashDeleteAsync(ConnectionsHash, connId);

            await _db.KeyDeleteAsync(UserKey(userId));
            await _db.SetRemoveAsync(OnlineUsersSet, userId.ToString());

            _logger.LogInformation("Cleared all Redis connections for user {UserId}", userId);
        }
    }
}