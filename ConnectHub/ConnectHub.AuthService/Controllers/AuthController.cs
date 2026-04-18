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
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(request);
            
            if (!result.Success)
                return Unauthorized(result);  // 401 Unauthorized

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
        /// LOGOUT - Update user's online status
        /// POST /api/auth/logout
        /// Requires authentication (must have valid JWT token)
        /// </summary>
        [Authorize]  // <-- This endpoint requires a valid JWT token
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