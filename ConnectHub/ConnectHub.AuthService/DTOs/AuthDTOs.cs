using System.ComponentModel.DataAnnotations;

namespace ConnectHub.AuthService.DTOs
{
    /// <summary>
    /// DATA TRANSFER OBJECTS (DTOs) - Like shipping boxes for data
    /// These define the exact shape of data sent between frontend and backend
    /// Think of them as contracts - both sides agree on the format
    /// This prevents sending too much or too little data
    /// </summary>

    // REQUEST DTOs (What frontend sends to backend)

    /// <summary>
    /// REGISTER REQUEST - What frontend must send to create an account
    /// All fields are required - frontend must provide all of them
    /// </summary>
    public class RegisterRequest
    {
        [Required(ErrorMessage = "Username is required")]
        [MinLength(3, ErrorMessage = "Username must be at least 3 characters")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Display name is required")]
        public string DisplayName { get; set; } = string.Empty;
    }

    /// <summary>
    /// LOGIN REQUEST - What frontend sends to log in
    /// User can enter either username OR email in the same field
    /// </summary>
    public class LoginRequest
    {
        [Required(ErrorMessage = "Username or email is required")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    ///  GOOGLE LOGIN REQUEST - What frontend sends for Google login
    /// Frontend gets ID token from Google's JavaScript library
    /// Then sends that token to us for verification
    /// </summary>
    public class GoogleLoginRequest
    {
        [Required(ErrorMessage = "Google ID token is required")]
        public string IdToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// UPDATE PROFILE REQUEST - What frontend sends to update profile
    /// All fields are optional (nullable) because user might update only some
    /// Example: Only change display name, leave bio and avatar as-is
    /// </summary>
    public class UpdateProfileRequest
    {
        public string? DisplayName { get; set; }  // null = don't change
        public string? Bio { get; set; }          // null = don't change
        public string? AvatarUrl { get; set; }    // null = don't change
    }

    /// <summary>
    /// CHANGE PASSWORD REQUEST - What frontend sends to change password
    /// Need current password for security (verify it's really the user)
    /// </summary>
    public class ChangePasswordRequest
    {
        [Required(ErrorMessage = "Current password is required")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(6, ErrorMessage = "New password must be at least 6 characters")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // ========== RESPONSE DTOs (What backend returns to frontend) ==========

    /// <summary>
    /// AUTH RESPONSE - What API returns after login or registration
    /// Includes success flag, human-readable message, JWT token, and user info
    /// </summary>
    public class AuthResponse
    {
        public bool Success { get; set; }          // Did it work? (true/false)
        public string Message { get; set; } = string.Empty;  // "Welcome back!"
        public string Token { get; set; } = string.Empty;    // JWT for future requests
        public UserDto? User { get; set; }         // User info (no password!)
    }

    /// <summary>
    /// USER DTO - User information (SAFE version for API responses)
    /// Notice: NO PasswordHash, NO GoogleId - sensitive data removed!
    /// This is what frontend can see about users
    /// </summary>
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Bio { get; set; }
        public string? AvatarUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// USER SEARCH RESPONSE - What API returns when searching for users
    /// Contains list of matching users and total count
    /// </summary>
    public class UserSearchResponse
    {
        public List<UserDto> Users { get; set; } = new();
        public int TotalCount { get; set; }
    }
}