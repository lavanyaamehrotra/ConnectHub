using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ConnectHub.NotificationService.DTOs;
using ConnectHub.NotificationService.Interfaces;
using System.Security.Claims;

namespace ConnectHub.NotificationService.Controllers
{
    // ============================================================
    // UC5 — NotificationController
    // FROM CLASS DIAGRAM:
    //   [ApiController][Route("api/notifications")]
    //   GET  byRecipient/unread/Count/all
    //   PUT  markAsRead/markAllRead
    //   DELETE
    //   POST sendBulk
    // ============================================================
    [ApiController]
    [Route("api/notifications")]
    [Authorize]
    public class NotificationController : ControllerBase
    {
        private readonly INotificationService _notificationService;

        public NotificationController(INotificationService notificationService)
        {
            _notificationService = notificationService;
        }

        // GET /api/notifications/byRecipient?page=1&pageSize=20
        // Returns paginated notifications for the logged-in user
        [HttpGet("byRecipient")]
        public async Task<IActionResult> GetByRecipient([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var items  = await _notificationService.GetByRecipientAsync(userId, page, pageSize);
            return Ok(items);
        }

        // GET /api/notifications/unread
        // Returns only unread notifications for the logged-in user
        [HttpGet("unread")]
        public async Task<IActionResult> GetUnread()
        {
            var userId = GetUserId();
            var items  = await _notificationService.GetUnreadAsync(userId);
            return Ok(items);
        }

        // GET /api/notifications/unreadCount
        // Returns unread badge count (used by frontend badge display)
        [HttpGet("unreadCount")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            var count  = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(new { count });
        }

        // GET /api/notifications/all?page=1&pageSize=50  (admin)
        [HttpGet("all")]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var items = await _notificationService.GetAllAsync(page, pageSize);
            return Ok(items);
        }

        // PUT /api/notifications/markAsRead/{id}
        [HttpPut("markAsRead/{notificationId:guid}")]
        public async Task<IActionResult> MarkAsRead(Guid notificationId)
        {
            await _notificationService.MarkAsReadAsync(notificationId);
            return Ok(new { message = "Marked as read" });
        }

        // PUT /api/notifications/markAllRead
        [HttpPut("markAllRead")]
        public async Task<IActionResult> MarkAllRead()
        {
            var userId = GetUserId();
            await _notificationService.MarkAllReadAsync(userId);
            return Ok(new { message = "All notifications marked as read" });
        }

        // DELETE /api/notifications/{id}
        [HttpDelete("{notificationId:guid}")]
        public async Task<IActionResult> Delete(Guid notificationId)
        {
            await _notificationService.DeleteNotificationAsync(notificationId);
            return Ok(new { message = "Notification deleted" });
        }

        // POST /api/notifications/sendBulk  (admin broadcast)
        [HttpPost("sendBulk")]
        public async Task<IActionResult> SendBulk([FromBody] BulkNotificationDto dto)
        {
            await _notificationService.SendBulkAsync(dto);
            return Ok(new { message = $"Sent to {dto.RecipientIds.Count} users" });
        }

        // POST /api/notifications/send  (called by other microservices internally)
        [HttpPost("send")]
        public async Task<IActionResult> Send([FromBody] CreateNotificationDto dto)
        {
            var result = await _notificationService.SendAsync(dto);
            return Ok(result);
        }

        // Helper: extract userId from JWT claim
        private Guid GetUserId()
        {
            var str = User.FindFirstValue(ClaimTypes.NameIdentifier)
                   ?? User.FindFirstValue("sub")
                   ?? "";
            return Guid.TryParse(str, out var id) ? id : Guid.Empty;
        }
    }
}
