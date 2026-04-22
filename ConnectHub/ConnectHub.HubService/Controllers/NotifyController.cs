using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ConnectHub.HubService.Hubs;

namespace ConnectHub.HubService.Controllers
{
    // ============================================================
    // UC5 — NotifyController (inside HubService)
    //
    // NotificationService calls POST /api/notify/badge after saving
    // a notification. This controller uses IHubContext to push the
    // updated unread count to the user's SignalR connection.
    //
    // FROM CLASS DIAGRAM:
    // "calls IHubContext<ChatHub>.Clients.User(recipientId)
    //  .SendAsync('NotificationCount', unreadCount)"
    // ============================================================
    [ApiController]
    [Route("api/notify")]
    public class NotifyController : ControllerBase
    {
        private readonly IHubContext<ChatHub> _hubContext;

        public NotifyController(IHubContext<ChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        // POST /api/notify/badge
        // Body: { "userId": "guid", "unreadCount": 5 }
        [HttpPost("badge")]
        public async Task<IActionResult> PushBadge([FromBody] BadgeUpdateRequest request)
        {
            // Push real-time badge count to the user's connected clients
            await _hubContext.Clients
                .User(request.UserId.ToString())
                .SendAsync("NotificationCount", request.UnreadCount);

            return Ok();
        }
    }

    public class BadgeUpdateRequest
    {
        public Guid UserId      { get; set; }
        public int  UnreadCount { get; set; }
    }
}