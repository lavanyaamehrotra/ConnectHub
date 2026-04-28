using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.AuthService.DTOs;
using ConnectHub.AuthService.Interfaces;

namespace ConnectHub.AuthService.Controllers
{
    /// <summary>
    /// USER CONTROLLER - Handles profile, search, and account management
    /// ALL endpoints in this controller require authentication
    /// The [Authorize] attribute ensures every request has a valid JWT token
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]  // <-- ALL endpoints in this controller require a valid token
    public class UserController : ControllerBase
    {
        private readonly IAuthService _authService;

        public UserController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// GET PROFILE - Get current user's information
        /// GET /api/user/profile
        /// Requires authentication
        /// </summary>
        [HttpGet("profile")]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetUserId();
            var user = await _authService.GetUserByIdAsync(userId);
            
            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(user);
        }

        /// <summary>
        /// UPDATE PROFILE - Update display name, bio, or avatar
        /// PUT /api/user/profile
        /// Requires authentication
        /// </summary>
        [HttpPut("profile")]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var userId = GetUserId();
            var result = await _authService.UpdateProfileAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// CHANGE PASSWORD - Update user's password
        /// POST /api/user/change-password
        /// Requires authentication
        /// </summary>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = GetUserId();
            var result = await _authService.ChangePasswordAsync(userId, request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// DEACTIVATE ACCOUNT - Soft delete your account
        /// DELETE /api/user/deactivate
        /// Requires authentication
        /// </summary>
        [HttpDelete("deactivate")]
        public async Task<IActionResult> DeactivateAccount()
        {
            var userId = GetUserId();
            var result = await _authService.DeactivateAccountAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        ///  SEARCH USERS - Find users by keyword
        /// GET /api/user/search?q=keyword
        /// Requires authentication
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string q)
        {
            var result = await _authService.SearchUsersAsync(q ?? "");
            return Ok(result);
        }

        /// <summary>
        /// GET USER BY ID - Get any user's public information
        /// GET /api/user/{id}
        /// AllowAnonymous - internal services need to fetch sender names for notifications
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            
            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(user);
        }

        /// <summary>
        /// GET USERS BY IDS — Bulk user lookup for recent chats enrichment
        /// POST /api/user/by-ids
        /// AllowAnonymous - internal service lookup
        /// </summary>
        [AllowAnonymous]
        [HttpPost("by-ids")]
        public async Task<IActionResult> GetUsersByIds([FromBody] List<Guid> userIds)
        {
            if (userIds == null || userIds.Count == 0)
                return Ok(new List<object>());

            var result = await _authService.GetUsersByIdsAsync(userIds);
            return Ok(result);
        }
        [AllowAnonymous]
        [HttpGet("{id}/email")]
        public async Task<IActionResult> GetUserEmail(Guid id)
        {
            var user = await _authService.GetUserByIdAsync(id);

            if (user == null)
                return NotFound();

            return Ok(new { Email = user.Email });
        }

        /// <summary>
        /// GET ALL USERS — For Admin Dashboard
        /// GET /api/user/all
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("all")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(users);
        }

        /// <summary>
        /// DEACTIVATE/ACTIVATE ACCOUNT — Admin action
        /// POST /api/user/{id}/toggle-active
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpPost("{id}/toggle-active")]
        public async Task<IActionResult> ToggleActive(Guid id)
        {
            var result = await _authService.ToggleUserStatusAsync(id);
            if (!result.Success) return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// UPDATE PRESENCE — For HubService sync
        /// POST /api/user/{id}/presence?isOnline=true
        /// </summary>
        [Authorize]
        [HttpPost("{id}/presence")]
        public async Task<IActionResult> UpdatePresence(Guid id, [FromQuery] bool isOnline)
        {
            var result = await _authService.SetUserOnlineStatusAsync(id, isOnline);
            return Ok(result);
        }

        /// <summary>
        /// HELPER - Extract User ID from JWT token
        /// </summary>
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}