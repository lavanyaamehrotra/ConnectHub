using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ConnectHub.AuthService.DTOs;
using ConnectHub.AuthService.Interfaces;

namespace ConnectHub.AuthService.Controllers
{
    /// <summary>
    /// AUTH CONTROLLER - Handles login, registration, and logout
    /// This is the "reception desk" for authentication requests
    /// Most endpoints are public (no token needed) except logout
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        /// <summary>
        /// REGISTER - Create a new user account
        /// POST /api/auth/register
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            // Check if the request data is valid (email format, password length, etc.)
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.RegisterAsync(request);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// LOGIN - Authenticate and get JWT token
        /// POST /api/auth/login
        /// Public endpoint - no authentication required
        /// Returns 200 with IsDeactivated=true if credentials correct but account off
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);

            // Return 200 even for deactivated accounts so frontend can read IsDeactivated flag
            if (!result.Success && !result.IsDeactivated)
                return Unauthorized(result);

            return Ok(result);
        }

        /// <summary>
        /// GOOGLE LOGIN - Authenticate with Google OAuth
        /// POST /api/auth/google-login
        /// Public endpoint - no authentication required
        /// </summary>
        [HttpPost("google-login")]
        public async Task<IActionResult> GoogleLogin([FromBody] GoogleLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.GoogleLoginAsync(request.IdToken);
            
            if (!result.Success)
                return Unauthorized(result);

            return Ok(result);
        }

        /// <summary>
        /// REACTIVATE - Re-enable a previously deactivated account
        /// POST /api/auth/reactivate
        /// Public endpoint - user already verified their password during login
        /// </summary>
        [HttpPost("reactivate")]
        public async Task<IActionResult> Reactivate([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ReactivateAccountAsync(request.UsernameOrEmail);

            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// LOGOUT - Update user's online status
        /// POST /api/auth/logout
        /// Requires authentication (must have valid JWT token)
        /// </summary>
        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = GetUserId();
            var result = await _authService.LogoutAsync(userId);
            
            if (!result.Success)
                return BadRequest(result);

            return Ok(result);
        }

        /// <summary>
        /// HELPER - Extract User ID from JWT token
        /// The token contains claims, one of which is NameIdentifier (UserId)
        /// </summary>
        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}