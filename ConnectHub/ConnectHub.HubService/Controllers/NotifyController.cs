using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ConnectHub.HubService.Hubs;

namespace ConnectHub.HubService.Controllers
{
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
        [HttpPost("badge")]
        public async Task<IActionResult> PushBadge([FromBody] BadgeUpdateRequest request)
        {
            await _hubContext.Clients
                .User(request.UserId.ToString())
                .SendAsync("NotificationCount", request.UnreadCount);

            return Ok();
        }

        // POST /api/notify/room-added
        [HttpPost("room-added")]
        public async Task<IActionResult> NotifyRoomAdded([FromBody] RoomAddedRequest request)
        { 
            await _hubContext.Clients
                .User(request.UserId.ToString())
                .SendAsync("NewRoomAdded", request.RoomId);

            return Ok();
        }
    }

    public class BadgeUpdateRequest
    {
        public Guid UserId      { get; set; }
        public int  UnreadCount { get; set; }
    }

    public class RoomAddedRequest
    {
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
    }
}