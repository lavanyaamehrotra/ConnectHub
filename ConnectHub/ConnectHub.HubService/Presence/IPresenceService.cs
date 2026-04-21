using ConnectHub.HubService.Models;

namespace ConnectHub.HubService.Presence
{
    // ============================================================
    // WHAT IS THIS FILE?
    // Interface (contract) for PresenceService.
    // Matches exactly the IPresenceService shown in class diagram.
    //
    // METHODS FROM CLASS DIAGRAM:
    // + UserConnected(int,string):void
    // + UserDisconnected(int,string):void
    // + GetConnectionsByUserId(int):IList<string>
    // + GetOnlineUserIds():IList<int>
    // + IsUserOnline(int):bool
    // + GetConnectionCount():int
    // + GetOnlineUsersInfo():IList<UserConnection>
    // + ClearUserConnections(int):void
    //
    // NOTE: The doc diagram shows "int" for userId because it was
    // written generically. Your codebase uses Guid for UserId,
    // so we use Guid here to match your actual AuthService User model.
    // ============================================================
    public interface IPresenceService
    {
        // Called from ChatHub.OnConnectedAsync()
        void UserConnected(Guid userId, string connectionId);

        // Called from ChatHub.OnDisconnectedAsync()
        void UserDisconnected(Guid userId, string connectionId);

        // Get all connectionIds for a user (for multi-device delivery)
        List<string> GetConnectionsByUserId(Guid userId);

        // Get all online user IDs
        List<Guid> GetOnlineUserIds();

        // True if user has at least one active connection
        bool IsUserOnline(Guid userId);

        // Total WebSocket connections across all users
        int GetConnectionCount();

        // Detailed connection info (with device/timestamp metadata)
        List<UserConnection> GetOnlineUsersInfo();

        // Force-remove all connections for a user (e.g. admin suspends account)
        void ClearUserConnections(Guid userId);
    }
}
