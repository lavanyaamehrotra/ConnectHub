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
        /// Requires authentication
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(Guid id)
        {
            var user = await _authService.GetUserByIdAsync(id);
            
            if (user == null)
                return NotFound(new { Message = "User not found" });

            return Ok(user);
        }
        /// <summary>
        /// GET USER EMAIL - Called internally by NotificationService
        /// GET /api/user/{id}/email
        /// AllowAnonymous - internal service-to-service call, no JWT needed
        /// </summary>
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
        /// HELPER - Extract User ID from JWT token
        /// </summary>
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}