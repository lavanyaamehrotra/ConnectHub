using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.MessageService.DTOs;
using ConnectHub.MessageService.Interfaces;

namespace ConnectHub.MessageService.Controllers
{
    [ApiController]
    [Route("api/messages")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _messageService;

        public MessagesController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.SendMessageAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("send-media")]
        public async Task<IActionResult> SendMediaMessage([FromBody] SendMediaMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.SendMediaMessageAsync(userId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("direct/{otherUserId}")]
        public async Task<IActionResult> GetDirectMessages(Guid otherUserId)
        {
            var userId = GetUserId();
            var result = await _messageService.GetDirectMessagesAsync(userId, otherUserId);
            return Ok(result);
        }

        [HttpGet("room/{roomId}")]
        public async Task<IActionResult> GetRoomMessages(Guid roomId)
        {
            var userId = GetUserId();
            var result = await _messageService.GetRoomMessagesAsync(userId, roomId);
            return Ok(result);
        }

        [HttpGet("unread")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            var result = await _messageService.GetUnreadCountAsync(userId);
            return Ok(new { UnreadCount = result });
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = GetUserId();
            var result = await _messageService.GetRecentChatsAsync(userId);
            return Ok(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string q)
        {
            var userId = GetUserId();
            var result = await _messageService.SearchMessagesAsync(userId, q ?? "");
            return Ok(result);
        }

        [HttpPut("markRead/{messageId}")]
        public async Task<IActionResult> MarkAsRead(Guid messageId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.MarkAsReadAsync(userId, messageId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("edit/{messageId}")]
        public async Task<IActionResult> EditMessage(Guid messageId, [FromBody] EditMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.EditMessageAsync(userId, messageId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("delete/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userId = GetUserId();
                var success = await _messageService.DeleteMessageAsync(userId, messageId);
                if (success)
                    return Ok(new { Success = true });
                return NotFound(new { Message = "Message not found or already deleted." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // Global mark all as read (across all conversations)
        [HttpPut("mark-all-read")]
        public async Task<IActionResult> MarkAllGlobalAsRead()
        {
            var userId = GetUserId();
            var result = await _messageService.MarkAllGlobalAsReadAsync(userId);
            return Ok(new { MarkedCount = result });
        }

        // Maintaining this for backward compatibility if the frontend still calls mark-all-read/{otherUserId}
        [HttpPut("mark-all-read/{otherUserId}")]
        public async Task<IActionResult> MarkAllAsRead(Guid otherUserId)
        {
            var userId = GetUserId();
            var result = await _messageService.MarkAllAsReadAsync(userId, otherUserId);
            return Ok(new { MarkedCount = result });
        }

        // Endpoint to get a specific message by Id
        [HttpGet("{messageId}")]
        public async Task<IActionResult> GetMessageById(Guid messageId)
        {
            try
            {
                var userId = GetUserId();
                var result = await _messageService.GetMessageByIdAsync(userId, messageId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.TryParse(userIdClaim, out var id) ? id : Guid.Empty;
        }
    }
}