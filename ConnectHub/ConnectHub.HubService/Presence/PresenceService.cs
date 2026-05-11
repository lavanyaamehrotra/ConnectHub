using StackExchange.Redis;
using System.Text.Json;
using ConnectHub.HubService.Models;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ConnectHub.HubService.Presence
{
    public class PresenceService : IPresenceService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PresenceService> _logger;
        
        // Local fallback if Redis is down
        private static readonly ConcurrentDictionary<Guid, HashSet<string>> _localPresence = new();

        private static string UserKey(Guid userId)      => $"presence:user:{userId}";
        private const  string ConnectionsHash           =  "presence:connections";
        private const  string OnlineUsersSet            =  "presence:online_users";
        private static readonly TimeSpan UserKeyTtl     = TimeSpan.FromHours(24);

        public PresenceService(IConnectionMultiplexer redis, ILogger<PresenceService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task UserConnectedAsync(Guid userId, string connectionId)
        {
            // 1. Local Fallback
            var connections = _localPresence.GetOrAdd(userId, _ => new HashSet<string>());
            lock (connections) { connections.Add(connectionId); }

            // 2. Redis
            try
            {
                var db = _redis.GetDatabase();
                await db.SetAddAsync(UserKey(userId), connectionId);
                await db.KeyExpireAsync(UserKey(userId), UserKeyTtl);

                var info = new UserConnection { ConnectionId = connectionId, UserId = userId, ConnectedAt = DateTime.UtcNow };
                await db.HashSetAsync(ConnectionsHash, connectionId, JsonSerializer.Serialize(info));
                await db.SetAddAsync(OnlineUsersSet, userId.ToString().ToLower());
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis UserConnectedAsync failed (using fallback): {Msg}", ex.Message);
            }
        }

        public async Task UserDisconnectedAsync(Guid userId, string connectionId)
        {
            // 1. Local Fallback
            if (_localPresence.TryGetValue(userId, out var connections))
            {
                lock (connections)
                {
                    connections.Remove(connectionId);
                    if (connections.Count == 0) _localPresence.TryRemove(userId, out _);
                }
            }

            // 2. Redis
            try
            {
                var db = _redis.GetDatabase();
                await db.SetRemoveAsync(UserKey(userId), connectionId);
                await db.HashDeleteAsync(ConnectionsHash, connectionId);

                var remaining = await db.SetLengthAsync(UserKey(userId));
                if (remaining == 0)
                {
                    await db.KeyDeleteAsync(UserKey(userId));
                    await db.SetRemoveAsync(OnlineUsersSet, userId.ToString().ToLower());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis UserDisconnectedAsync failed (using fallback): {Msg}", ex.Message);
            }
        }

        public async Task<bool> IsUserOnlineAsync(Guid userId)
        {
            // 1. Check Local Fallback
            if (_localPresence.ContainsKey(userId)) return true;

            // 2. Check Redis
            try
            {
                var db = _redis.GetDatabase();
                return await db.SetContainsAsync(OnlineUsersSet, userId.ToString().ToLower());
            }
            catch
            {
                return false;
            }
        }

        public async Task<List<Guid>> GetOnlineUserIdsAsync()
        {
            var results = new HashSet<Guid>();

            // 1. Local Fallback
            foreach (var id in _localPresence.Keys) results.Add(id);

            // 2. Redis
            try
            {
                var db = _redis.GetDatabase();
                var members = await db.SetMembersAsync(OnlineUsersSet);
                foreach (var m in members)
                {
                    if (Guid.TryParse(m.ToString(), out var guid)) results.Add(guid);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Redis GetOnlineUserIdsAsync failed: {Msg}", ex.Message);
            }

            return results.ToList();
        }

        public async Task<List<string>> GetConnectionsByUserIdAsync(Guid userId)
        {
             if (_localPresence.TryGetValue(userId, out var connections)) 
                lock(connections) return connections.ToList();
             
             try {
                var db = _redis.GetDatabase();
                var members = await db.SetMembersAsync(UserKey(userId));
                return members.Select(m => m.ToString()).ToList();
             } catch { return new List<string>(); }
        }

        public async Task PurgeStalePresenceAsync()
        {
            try {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(ConnectionsHash);
                await db.KeyDeleteAsync(OnlineUsersSet);
            } catch {}
        }

        public async Task<int> GetConnectionCountAsync() => (await GetOnlineUserIdsAsync()).Count;
        
        public async Task<List<UserConnection>> GetOnlineUsersInfoAsync() 
        {
            try {
                var db = _redis.GetDatabase();
                var entries = await db.HashGetAllAsync(ConnectionsHash);
                return entries.Select(e => JsonSerializer.Deserialize<UserConnection>(e.Value.ToString())!).ToList();
            } catch { return new List<UserConnection>(); }
        }

        public async Task ClearUserConnectionsAsync(Guid userId) 
        { 
            _localPresence.TryRemove(userId, out _); 
            try {
                var db = _redis.GetDatabase();
                await db.KeyDeleteAsync(UserKey(userId));
                await db.SetRemoveAsync(OnlineUsersSet, userId.ToString().ToLower());
            } catch {}
        }
    }
}