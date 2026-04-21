using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.MessageService.DTOs;
using ConnectHub.MessageService.Interfaces;

namespace ConnectHub.MessageService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var userId = GetUserId();
            var result = await _messageService.SendMessageAsync(userId, request);
            return Ok(result);
        }

        [HttpPost("send-media")]
        public async Task<IActionResult> SendMediaMessage([FromBody] SendMediaMessageRequest request)
        {
            var userId = GetUserId();
            var result = await _messageService.SendMediaMessageAsync(userId, request);
            return Ok(result);
        }

        [HttpGet("conversation/{otherUserId}")]
        public async Task<IActionResult> GetConversation(Guid otherUserId)
        {
            var userId = GetUserId();
            var result = await _messageService.GetConversationAsync(userId, otherUserId);
            return Ok(result);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentChats()
        {
            var userId = GetUserId();
            var result = await _messageService.GetRecentChatsAsync(userId);
            return Ok(result);
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = GetUserId();
            var result = await _messageService.GetUnreadCountAsync(userId);
            return Ok(new { UnreadCount = result });
        }

        [HttpPut("mark-all-read/{otherUserId}")]
        public async Task<IActionResult> MarkAllAsRead(Guid otherUserId)
        {
            var userId = GetUserId();
            var result = await _messageService.MarkAllAsReadAsync(userId, otherUserId);
            return Ok(new { MarkedCount = result });
        }

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
                await _messageService.DeleteMessageAsync(userId, messageId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("read/{messageId}")]
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

        [HttpGet("search")]
        public async Task<IActionResult> SearchMessages([FromQuery] string q)
        {
            var userId = GetUserId();
            var result = await _messageService.SearchMessagesAsync(userId, q ?? "");
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}