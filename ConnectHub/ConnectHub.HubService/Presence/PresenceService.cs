using System.Collections.Concurrent;
using ConnectHub.HubService.Models;

namespace ConnectHub.HubService.Presence
{
    // ============================================================
    // WHAT IS THIS FILE?
    // Implementation of IPresenceService.
    // Matches the class diagram which shows TWO dictionaries:
    //
    // _connections: ConcurrentDictionary<Guid, HashSet<string>>
    //   Key   = UserId
    //   Value = set of active connectionIds for that user
    //   → Answers: "Is this user online? Which connections do they have?"
    //
    // _userInfo: ConcurrentDictionary<string, UserConnection>
    //   Key   = connectionId
    //   Value = UserConnection (userId + timestamp + device)
    //   → Answers: "Who owns this connectionId? When did they connect?"
    //
    // WHY SINGLETON?
    // All Hub instances share ONE PresenceService instance.
    // If Scoped: each connection gets a fresh empty dictionary = broken.
    //
    // WHY TWO DICTIONARIES?
    // _connections: fast lookup by userId → "is John online?"
    // _userInfo:    fast lookup by connectionId → "who is conn-abc?"
    //               also provides rich metadata for admin monitoring
    // ============================================================
    public class PresenceService : IPresenceService
    {
        // userId → { connectionId1, connectionId2, ... }
        // One user can have multiple connections (multi-device)
        private readonly ConcurrentDictionary<Guid, HashSet<string>> _connections
            = new ConcurrentDictionary<Guid, HashSet<string>>();

        // connectionId → UserConnection metadata
        // Fast reverse lookup: "who owns this connection?"
        private readonly ConcurrentDictionary<string, UserConnection> _userInfo
            = new ConcurrentDictionary<string, UserConnection>();

        // Lock for HashSet operations (HashSet is not thread-safe itself)
        private readonly object _lock = new object();

        private readonly ILogger<PresenceService> _logger;

        public PresenceService(ILogger<PresenceService> logger)
        {
            _logger = logger;
        }

        // -----------------------------------------------------------
        // Called from ChatHub.OnConnectedAsync()
        //
        // Adds the connectionId to the user's active connections set.
        // Also stores metadata in _userInfo for admin monitoring.
        // -----------------------------------------------------------
        public void UserConnected(Guid userId, string connectionId)
        {
            lock (_lock)
            {
                var connections = _connections.GetOrAdd(userId, _ => new HashSet<string>());
                connections.Add(connectionId);
            }

            // Store connection metadata for GetOnlineUsersInfo()
            _userInfo[connectionId] = new UserConnection
            {
                ConnectionId = connectionId,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            };

            _logger.LogInformation("User {UserId} connected. ConnId: {ConnId}", userId, connectionId);
        }

        // -----------------------------------------------------------
        // Called from ChatHub.OnDisconnectedAsync()
        //
        // Removes this one connectionId. If it was the user's last
        // connection, removes them from _connections entirely.
        // -----------------------------------------------------------
        public void UserDisconnected(Guid userId, string connectionId)
        {
            lock (_lock)
            {
                if (_connections.TryGetValue(userId, out var connections))
                {
                    connections.Remove(connectionId);

                    if (connections.Count == 0)
                    {
                        _connections.TryRemove(userId, out _);
                        _logger.LogInformation("User {UserId} is now OFFLINE", userId);
                    }
                }
            }

            // Remove metadata entry for this connection
            _userInfo.TryRemove(connectionId, out _);
        }

        // -----------------------------------------------------------
        // Get all connectionIds for a specific user.
        // Used when we need to target ALL devices of a user.
        // (Clients.User() handles this automatically, but this method
        //  is useful for admin monitoring and debugging.)
        // -----------------------------------------------------------
        public List<string> GetConnectionsByUserId(Guid userId)
        {
            if (_connections.TryGetValue(userId, out var connections))
            {
                lock (_lock)
                {
                    return connections.ToList();
                }
            }
            return new List<string>();
        }

        // -----------------------------------------------------------
        // Returns all currently online user IDs.
        // Sent to newly connected clients for their contact list.
        // -----------------------------------------------------------
        public List<Guid> GetOnlineUserIds()
        {
            return _connections.Keys.ToList();
        }

        // -----------------------------------------------------------
        // True if user has at least ONE active connection.
        // Used before broadcasting UserOffline — only do it when
        // this was the user's LAST connection.
        // -----------------------------------------------------------
        public bool IsUserOnline(Guid userId)
        {
            return _connections.ContainsKey(userId);
        }

        // -----------------------------------------------------------
        // Total count of ALL active WebSocket connections.
        // John on phone + laptop = 2 connections, 1 online user.
        // -----------------------------------------------------------
        public int GetConnectionCount()
        {
            lock (_lock)
            {
                return _connections.Values.Sum(set => set.Count);
            }
        }

        // -----------------------------------------------------------
        // Detailed info about all active connections.
        // Returns the _userInfo dictionary values.
        // Used by PresenceController for admin monitoring.
        // -----------------------------------------------------------
        public List<UserConnection> GetOnlineUsersInfo()
        {
            return _userInfo.Values.ToList();
        }

        // -----------------------------------------------------------
        // Force-clear all connections for a user.
        // Called when admin suspends/deactivates a user account.
        // Removes from both dictionaries.
        // -----------------------------------------------------------
        public void ClearUserConnections(Guid userId)
        {
            lock (_lock)
            {
                if (_connections.TryRemove(userId, out var connections))
                {
                    foreach (var connId in connections)
                        _userInfo.TryRemove(connId, out _);
                }
            }

            _logger.LogInformation("Cleared all connections for user {UserId}", userId);
        }
    }
}
