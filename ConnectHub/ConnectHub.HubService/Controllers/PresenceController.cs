using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConnectHub.HubService.Presence;

namespace ConnectHub.HubService.Controllers
{
    // ============================================================
    // UC4 Redis Update — PresenceController
    // Controller methods are now async to match the async
    // IPresenceService interface (Redis I/O).
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

        // GET /api/presence/online — list of online user IDs
        [HttpGet("online")]
        public async Task<IActionResult> GetOnlineUsers()
        {
            var users = await _presenceService.GetOnlineUserIdsAsync();
            return Ok(users);
        }

        // GET /api/presence/count — total connection count
        [HttpGet("count")]
        public async Task<IActionResult> GetConnectionCount()
        {
            var count = await _presenceService.GetConnectionCountAsync();
            return Ok(new { count });
        }

        // GET /api/presence/info — detailed connection info (admin)
        [HttpGet("info")]
        public async Task<IActionResult> GetConnectionInfo()
        {
            var info = await _presenceService.GetOnlineUsersInfoAsync();
            return Ok(info);
        }

        // GET /api/presence/user/{userId} — is a specific user online?
        [HttpGet("user/{userId:guid}")]
        public async Task<IActionResult> IsUserOnline(Guid userId)
        {
            var online = await _presenceService.IsUserOnlineAsync(userId);
            return Ok(new { userId, online });
        }

        // DELETE /api/presence/user/{userId} — admin: force disconnect user
        [HttpDelete("user/{userId:guid}")]
        public async Task<IActionResult> ClearUser(Guid userId)
        {
            await _presenceService.ClearUserConnectionsAsync(userId);
            return Ok(new { message = $"Cleared all connections for user {userId}" });
        }
    }
}