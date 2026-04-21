using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.ChatRoomService.DTOs;
using ConnectHub.ChatRoomService.Interfaces;

namespace ConnectHub.ChatRoomService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
            var result = await _chatRoomService.CreateRoomAsync(userId, request);
            return Ok(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMyRooms()
        {
            var userId = GetUserId();
            var result = await _chatRoomService.GetUserRoomsAsync(userId);
            return Ok(result);
        }

        [HttpGet("public")]
        public async Task<IActionResult> GetPublicRooms()
        {
            var result = await _chatRoomService.GetPublicRoomsAsync();
            return Ok(result);
        }

        [HttpGet("{roomId}")]
        public async Task<IActionResult> GetRoom(Guid roomId)
        {
            var result = await _chatRoomService.GetRoomAsync(roomId);
            return Ok(result);
        }

        [HttpGet("{roomId}/member-count")]
        public async Task<IActionResult> GetMemberCount(Guid roomId)
        {
            var result = await _chatRoomService.GetMemberCountAsync(roomId);
            return Ok(new { Count = result });
        }

        [HttpGet("{roomId}/is-member")]
        public async Task<IActionResult> IsUserInRoom(Guid roomId)
        {
            var userId = GetUserId();
            var result = await _chatRoomService.IsUserInRoomAsync(userId, roomId);
            return Ok(new { IsMember = result });
        }

        [HttpPut("{roomId}")]
        public async Task<IActionResult> UpdateRoom(Guid roomId, [FromBody] UpdateRoomRequest request)
        {
            try
            {
                var userId = GetUserId();
                var result = await _chatRoomService.UpdateRoomAsync(userId, roomId, request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{roomId}")]
        public async Task<IActionResult> DeleteRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.DeleteRoomAsync(userId, roomId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        // ========== MEMBER MANAGEMENT ==========

        [HttpPost("{roomId}/join")]
        public async Task<IActionResult> JoinRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.JoinRoomAsync(userId, roomId);
                return Ok(new { Success = true, Message = "Joined room successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{roomId}/leave")]
        public async Task<IActionResult> LeaveRoom(Guid roomId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.LeaveRoomAsync(userId, roomId);
                return Ok(new { Success = true, Message = "Left room successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpGet("{roomId}/members")]
        public async Task<IActionResult> GetMembers(Guid roomId)
        {
            var result = await _chatRoomService.GetRoomMembersAsync(roomId);
            return Ok(result);
        }

        [HttpPost("{roomId}/members")]
        public async Task<IActionResult> AddMember(Guid roomId, [FromBody] AddMemberRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.AddMemberAsync(userId, roomId, request);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpDelete("{roomId}/members/{memberUserId}")]
        public async Task<IActionResult> RemoveMember(Guid roomId, Guid memberUserId)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.RemoveMemberAsync(userId, roomId, memberUserId);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPost("{roomId}/admins")]
        public async Task<IActionResult> MakeAdmin(Guid roomId, [FromBody] MakeAdminRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.MakeAdminAsync(userId, roomId, request);
                return Ok(new { Success = true });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
        }

        [HttpPut("{roomId}/members/role")]
        public async Task<IActionResult> UpdateMemberRole(Guid roomId, [FromBody] UpdateMemberRoleRequest request)
        {
            try
            {
                var userId = GetUserId();
                await _chatRoomService.UpdateMemberRoleAsync(userId, roomId, request);
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
                var result = await _chatRoomService.SendMessageAsync(userId, roomId, request);
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
            var result = await _chatRoomService.GetRoomMessagesAsync(roomId, page, pageSize);
            return Ok(result);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}