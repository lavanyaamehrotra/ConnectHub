using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConnectHub.HubService.Presence;

namespace ConnectHub.HubService.Controllers
{
    // ============================================================
    // FROM CLASS DIAGRAM (PresenceController):
    // [ApiController][Route("api/presence")]
    //
    // + PresenceController(IPresenceService)
    // + GetOnlineUsers()         : IActionResult   → GET /api/presence/online-users
    // + IsOnline(userId)         : IActionResult   → GET /api/presence/is-online/{userId}
    // + GetConnectionCount()     : IActionResult   → GET /api/presence/connection-count
    // + GetOnlineUsersInfo()     : IActionResult   → GET /api/presence/online-users-info
    // + GetOnlineUsersInfo(userId): IActionResult  → GET /api/presence/online-users-info/{userId}
    //
    // WHY REST AND NOT JUST SIGNALR?
    // On page load SignalR hasn't connected yet. Other microservices
    // (e.g. NotificationService) also need to query presence over HTTP.
    // IPresenceService is Singleton → reads the same in-memory state
    // that ChatHub writes to.
    // ============================================================
    [ApiController]
    [Route("api/presence")]
    [Authorize]
    public class PresenceController : ControllerBase
    {
        private readonly IPresenceService _presenceService;

        public PresenceController(IPresenceService presenceService)
        {
            _presenceService = presenceService;
        }

        // ── GET /api/presence/online-users ───────────────────────
        // Returns all currently online user IDs.
        // Frontend uses this on page load to render green dots.
        [HttpGet("online-users")]
        public IActionResult GetOnlineUsers()
        {
            var onlineUsers = _presenceService.GetOnlineUserIds();
            return Ok(new
            {
                OnlineUserIds = onlineUsers,
                Count         = onlineUsers.Count
            });
        }

        // ── GET /api/presence/is-online/{userId} ─────────────────
        // FROM CLASS DIAGRAM: IsOnline(int):IActionResult
        // Check whether one specific user is currently online.
        // Used when opening a chat window.
        [HttpGet("is-online/{userId:guid}")]
        public IActionResult IsOnline(Guid userId)
        {
            var isOnline = _presenceService.IsUserOnline(userId);
            return Ok(new
            {
                UserId   = userId,
                IsOnline = isOnline
            });
        }

        // ── GET /api/presence/connection-count ───────────────────
        // FROM CLASS DIAGRAM: GetConnectionCount():IActionResult
        // Total active WebSocket connections (for admin analytics).
        // Example: John on phone + laptop = 2 connections, 1 unique user.
        [HttpGet("connection-count")]
        public IActionResult GetConnectionCount()
        {
            var totalConnections = _presenceService.GetConnectionCount();
            var uniqueUsers      = _presenceService.GetOnlineUserIds().Count;
            return Ok(new
            {
                TotalConnections  = totalConnections,
                UniqueOnlineUsers = uniqueUsers
            });
        }

        // ── GET /api/presence/online-users-info ──────────────────
        // FROM CLASS DIAGRAM: GetOnlineUsersInfo():IActionResult
        // Detailed connection info for ALL active connections.
        // Used by admin monitoring dashboard.
        [HttpGet("online-users-info")]
        public IActionResult GetOnlineUsersInfo()
        {
            var info = _presenceService.GetOnlineUsersInfo();
            return Ok(new
            {
                Connections = info,
                Count       = info.Count
            });
        }

        // ── GET /api/presence/online-users-info/{userId} ─────────
        // FROM CLASS DIAGRAM: GetOnlineUsersInfo(int userId):IActionResult
        // Detailed connection info for ONE specific user
        // (all their active connections / devices).
        [HttpGet("online-users-info/{userId:guid}")]
        public IActionResult GetOnlineUsersInfo(Guid userId)
        {
            // Get all connection metadata then filter to this user's connections
            var connections = _presenceService.GetOnlineUsersInfo()
                .Where(c => c.UserId == userId)
                .ToList();

            return Ok(new
            {
                UserId      = userId,
                IsOnline    = _presenceService.IsUserOnline(userId),
                Connections = connections,
                Count       = connections.Count
            });
        }
    }
}