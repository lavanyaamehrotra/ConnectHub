namespace ConnectHub.HubService.Models
{
    // ============================================================
    // WHAT IS THIS FILE?
    // Represents one active WebSocket connection.
    // A single user can have MULTIPLE UserConnection entries
    // (one per open tab/device).
    //
    // WHERE IS IT STORED?
    // Inside PresenceService._userInfo dictionary:
    //   ConcurrentDictionary<string, UserConnection>
    //   Key = connectionId (SignalR's unique ID per WebSocket)
    //   Value = this object (who owns that connection + metadata)
    //
    // EXAMPLE:
    // John is on phone AND laptop:
    //   "phone-conn-abc" → UserConnection { UserId=JohnGuid, DeviceInfo="iPhone" }
    //   "laptop-conn-xyz" → UserConnection { UserId=JohnGuid, DeviceInfo="Chrome" }
    // ============================================================
    public class UserConnection
    {
        // SignalR's unique ID for this specific WebSocket connection
        public string ConnectionId { get; set; } = string.Empty;

        // The user who owns this connection (Guid matches AuthService User.UserId)
        public Guid UserId { get; set; }

        // When this connection was opened
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;

        // Optional: browser/device info from User-Agent header
        // Useful for debugging multi-device issues
        public string? DeviceInfo { get; set; }
    }
}
