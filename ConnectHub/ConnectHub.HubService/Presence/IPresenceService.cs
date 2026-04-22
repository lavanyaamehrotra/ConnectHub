using ConnectHub.HubService.Models;

namespace ConnectHub.HubService.Presence
{
    // ============================================================
    // UC4 - Redis Update
    // Methods are now async because Redis calls are I/O-bound.
    // Previously: ConcurrentDictionary (in-memory, single server).
    // Now: Redis (shared across multiple HubService instances).
    // ============================================================
    public interface IPresenceService
    {
        Task UserConnectedAsync(Guid userId, string connectionId);
        Task UserDisconnectedAsync(Guid userId, string connectionId);
        Task<List<string>> GetConnectionsByUserIdAsync(Guid userId);
        Task<List<Guid>> GetOnlineUserIdsAsync();
        Task<bool> IsUserOnlineAsync(Guid userId);
        Task<int> GetConnectionCountAsync();
        Task<List<UserConnection>> GetOnlineUsersInfoAsync();
        Task ClearUserConnectionsAsync(Guid userId);
    }
}