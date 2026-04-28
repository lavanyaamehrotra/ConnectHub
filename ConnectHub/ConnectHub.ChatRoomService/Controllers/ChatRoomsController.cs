using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.ChatRoomService.DTOs;
using ConnectHub.ChatRoomService.Interfaces;

namespace ConnectHub.ChatRoomService.Controllers
{
    [ApiController]
    [Route("api/rooms")]
    [Authorize]
    public class ChatRoomsController : ControllerBase
    {
        private readonly IChatRoomService _chatRoomService;

        public ChatRoomsController(IChatRoomService chatRoomService)
        {
            _chatRoomService = chatRoomService;
        }

        // ========== ROOM MANAGEMENT ==========

        [HttpPost("create")]
        public async Task<IActionResult> CreateRoom([FromBody] CreateRoomRequest request)
        {
            var userId = GetUserId();
            var username = GetUsername();
            var result = await _chatRoomService.CreateRoom(userId, username, request);
            return Ok(result);
        }

        [HttpGet("by-user")]
        public async Task<IActionResult> GetRoomsByUser()
        {
            var userId = GetUserId();
            var result = await _chatRoomService.GetRoomsByUser(userId);
            return Ok(result);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicRooms()
        {
            var result = await _chatRoomService.GetPublicRooms();
            return Ok(result);
        }

        [HttpGet("by-id/{roomId}")]
        public async Task<IActionResult> GetRoomById(Guid roomId)
        {
            var result = await _chatRoomService.GetRoomById(roomId);
            return Ok(result);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateRoom([FromQuery] Guid roomId, [FromBody] UpdateRoomRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatRoomService.UpdateRoom(userId, roomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("room/{roomId}")]
        public async Task<IActionResult> DeleteRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.DeleteRoom(userId, roomId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ========== MEMBER MANAGEMENT ==========

        [HttpPost("join/{roomId}")]
        public async Task<IActionResult> JoinRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                var username = GetUsername();
                await _chatRoomService.JoinRoom(userId, username, roomId);
                return Ok(new { Success = true, Message = "Joined room successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("leave/{roomId}")]
        public async Task<IActionResult> LeaveRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.LeaveRoom(userId, roomId);
                return Ok(new { Success = true, Message = "Left room successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("members/{roomId}")]
        public async Task<IActionResult> GetMembers(Guid roomId)
        {
            var result = await _chatRoomService.GetMembers(roomId);
            return Ok(result);
        }

        [HttpPost("addMember")]
        public async Task<IActionResult> AddMember([FromQuery] Guid roomId, [FromBody] AddMemberRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.AddMember(userId, roomId, request);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("removeMember")]
        public async Task<IActionResult> RemoveMember([FromQuery] Guid roomId, [FromQuery] Guid memberUserId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.RemoveMember(userId, roomId, memberUserId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("memberRole")]
        public async Task<IActionResult> UpdateMemberRole([FromQuery] Guid roomId, [FromBody] UpdateMemberRoleRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.UpdateMemberRole(userId, roomId, request);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ========== MESSAGING ==========

        [HttpPost("{roomId}/messages")]
        public async Task<IActionResult> SendMessage(Guid roomId, [FromBody] SendRoomMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatRoomService.SendMessage(userId, roomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{roomId}/messages")]
        public async Task<IActionResult> GetMessages(Guid roomId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50)
        {
            var result = await _chatRoomService.GetRoomMessages(roomId, page, pageSize);
            return Ok(result);
        }

        [HttpPut("messages/{messageId}")]
        public async Task<IActionResult> UpdateMessage(Guid messageId, [FromBody] SendRoomMessageRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatRoomService.UpdateMessage(userId, messageId, request.Content ?? "");
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            try
            {
                var userId = GetUserId();
                var roomId = await _chatRoomService.DeleteMessage(userId, messageId);
                return Ok(new { Success = true, RoomId = roomId });
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

        private string GetUsername()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown User";
        }
    }
}