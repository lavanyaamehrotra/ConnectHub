using ConnectHub.AuthService.DTOs;

namespace ConnectHub.AuthService.Interfaces
{
    /// <summary>
    /// AUTH SERVICE INTERFACE - The "menu" of authentication operations
    /// This defines WHAT authentication features are available
    /// The actual business logic is in Services/AuthService.cs
    /// Separating interface from implementation makes code testable and clean
    /// </summary>
    public interface IAuthService
    {
        // ==========  AUTHENTICATION METHODS ==========
        
        /// <summary>Register a new user with email and password</summary>
        Task<AuthResponse> RegisterAsync(RegisterRequest request);
        
        /// <summary>Login with email/password and receive JWT token</summary>
        Task<AuthResponse> LoginAsync(LoginRequest request);
        
        /// <summary>Login or register using Google OAuth</summary>
        Task<AuthResponse> GoogleLoginAsync(string idToken);
        
        /// <summary>Logout user (updates online status)</summary>
        Task<AuthResponse> LogoutAsync(Guid userId);
        
        // ==========  PROFILE MANAGEMENT ==========
        
        /// <summary>Update user's display name, bio, or avatar</summary>
        Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
        
        /// <summary>Change user's password (requires current password)</summary>
        Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
        
        /// <summary>Deactivate user account (soft delete)</summary>
        Task<AuthResponse> DeactivateAccountAsync(Guid userId);
        
        // ==========  USER QUERIES ==========
        
        /// <summary>Search for users by keyword</summary>
        Task<UserSearchResponse> SearchUsersAsync(string searchTerm);
        
        /// <summary>Get user details by ID</summary>
        Task<UserDto?> GetUserByIdAsync(Guid userId);
    }
}