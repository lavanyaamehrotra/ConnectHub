using BCrypt.Net;
using ConnectHub.AuthService.DTOs;
using ConnectHub.AuthService.Helpers;
using ConnectHub.AuthService.Models;
using ConnectHub.AuthService.Interfaces;
using ConnectHub.AuthService.Repositories;
using Microsoft.Extensions.Logging;

namespace ConnectHub.AuthService.Services
{
    /// <summary>
    /// AUTH SERVICE - The brain of your application
    /// This class contains ALL the business logic for authentication
    /// It validates inputs, hashes passwords, generates tokens, etc.
    /// Implements the IAuthService interface (the contract)
    /// Think of this as the manager that decides what happens and when
    /// </summary>
    public class AuthService : IAuthService
    {
        // Dependencies (things this service needs to do its job)
        private readonly IUserRepository _userRepository;  // To save/load users
        private readonly JwtHelper _jwtHelper;             // To create JWT tokens
        private readonly GoogleAuthHelper _googleAuthHelper; // To verify Google
        private readonly ILogger<AuthService> _logger;     // To log errors

        /// <summary>
        /// Constructor - Receives all dependencies via Dependency Injection
        /// .NET automatically provides these when the service is created
        /// </summary>
        public AuthService(
            IUserRepository userRepository,
            JwtHelper jwtHelper,
            GoogleAuthHelper googleAuthHelper,
            ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
            _googleAuthHelper = googleAuthHelper;
            _logger = logger;
        }

        /// <summary>
        /// REGISTER - Create a new user account
        /// Steps: 1. Check if username/email exists
        ///        2. Hash the password (scramble it)
        ///        3. Save to database
        ///        4. Generate JWT token
        ///        5. Return success with token
        /// </summary>
        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            try
            {
                // Check if username is already taken
                if (await _userRepository.UsernameExistsAsync(request.Username))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Username already taken. Please choose another."
                    };
                }

                // Check if email is already registered
                if (await _userRepository.EmailExistsAsync(request.Email))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email already registered. Try logging in instead."
                    };
                }

                // Create new user object
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password), // ✅ FIXED
                    DisplayName = request.DisplayName,
                    IsOnline = true,
                    LastSeen = DateTime.UtcNow,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Save to database
                await _userRepository.CreateAsync(user);
                
                // Generate JWT token for automatic login
                var token = _jwtHelper.GenerateToken(user);

                // Log success
                _logger.LogInformation("New user registered: {Username}", user.Username);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Registration successful! Welcome aboard! 🎉",
                    Token = token,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Username}", request.Username);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Registration failed due to server error. Please try again."
                };
            }
        }

        /// <summary>
        /// LOGIN - Authenticate user and return JWT token
        /// Steps: 1. Find user by username/email
        ///        2. Verify password (compare with hashed version)
        ///        3. Update online status
        ///        4. Generate JWT token
        ///        5. Return token to client
        /// </summary>
        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                // Find user by username OR email
                var user = await _userRepository.GetByUsernameOrEmailAsync(request.UsernameOrEmail);

                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username/email or password"
                    };
                }

                // Verify password before revealing account status
                if (string.IsNullOrEmpty(user.PasswordHash) ||
                    !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid username/email or password"
                    };
                }

                // Correct credentials but account is deactivated
                if (!user.IsActive)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        IsDeactivated = true,
                        Message = "Your account has been deactivated. Would you like to reactivate it?"
                    };
                }

                // Update online status (important for chat!)
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                // Generate JWT token
                var token = _jwtHelper.GenerateToken(user);

                _logger.LogInformation("User logged in: {Username}", user.Username);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successful! Welcome back! 👋",
                    Token = token,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for {UsernameOrEmail}", request.UsernameOrEmail);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Login failed due to server error"
                };
            }
        }

        /// <summary>
        /// GOOGLE LOGIN - Authenticate using Google OAuth
        /// Steps: 1. Verify Google token with Google's servers
        ///        2. Find or create user based on Google info
        ///        3. Update online status
        ///        4. Generate JWT token
        ///        5. Return token to client
        /// </summary>
        public async Task<AuthResponse> GoogleLoginAsync(string idToken)
        {
            try
            {
                // Verify the Google token is genuine
                var payload = await _googleAuthHelper.VerifyGoogleToken(idToken);
                if (payload == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Invalid Google token. Please try again."
                    };
                }

                // Check if user exists by Google ID
                var user = await _userRepository.GetByGoogleIdAsync(payload.Subject);
                
                if (user == null)
                {
                    // Check if user exists by email
                    user = await _userRepository.GetByEmailAsync(payload.Email);
                    
                    if (user == null)
                    {
                        // Create new user from Google data
                        var baseUsername = payload.Email.Split('@')[0];
                        var username = baseUsername;
                        var counter = 1;
                        
                        // Ensure username is unique
                        while (await _userRepository.UsernameExistsAsync(username))
                        {
                            username = $"{baseUsername}{counter}";
                            counter++;
                        }

                        user = new User
                        {
                            Username = username,
                            Email = payload.Email,
                            GoogleId = payload.Subject,
                            DisplayName = payload.Name ?? baseUsername,
                            AvatarUrl = payload.Picture,
                            IsOnline = true,
                            LastSeen = DateTime.UtcNow,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };
                        
                        await _userRepository.CreateAsync(user);
                        _logger.LogInformation("New Google user created: {Email}", payload.Email);
                    }
                    else
                    {
                        // Link Google ID to existing email account
                        // Also auto-reactivate if account was deactivated
                        user.GoogleId = payload.Subject;
                        user.IsActive = true; // auto-reactivate via Google identity
                        user.IsOnline = true;
                        user.LastSeen = DateTime.UtcNow;
                        await _userRepository.UpdateAsync(user);
                        _logger.LogInformation("Google account linked to existing user: {Email}", payload.Email);
                    }
                }
                else
                {
                    // If account was deactivated, auto-reactivate on Google login
                    // Google OAuth itself proves the user's identity, so we trust it
                    if (!user.IsActive)
                    {
                        user.IsActive = true;
                        _logger.LogInformation("Google user account auto-reactivated: {Email}", user.Email);
                    }

                    // Update online status
                    user.IsOnline = true;
                    user.LastSeen = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                }

                // Generate JWT token
                var token = _jwtHelper.GenerateToken(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Google login successful!",
                    Token = token,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Google login failed");
                return new AuthResponse
                {
                    Success = false,
                    Message = "Google login failed"
                };
            }
        }

        /// <summary>
        /// LOGOUT - Update user's online status
        /// Note: JWT tokens are stateless, so we don't "invalidate" them
        /// We just update the user's status to offline in the database
        /// The client should discard the token on their end
        /// </summary>
        public async Task<AuthResponse> LogoutAsync(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsOnline = false;
                    user.LastSeen = DateTime.UtcNow;
                    await _userRepository.UpdateAsync(user);
                    _logger.LogInformation("User logged out: {Username}", user.Username);
                }

                return new AuthResponse
                {
                    Success = true,
                    Message = "Logged out successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Logout failed for user {UserId}", userId);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Logout failed"
                };
            }
        }

        public async Task<AuthResponse> SetUserOnlineStatusAsync(Guid userId, bool isOnline)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null) return new AuthResponse { Success = false, Message = "User not found" };

                user.IsOnline = isOnline;
                user.LastSeen = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                return new AuthResponse { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to set online status for user {UserId}", userId);
                return new AuthResponse { Success = false, Message = "Update failed" };
            }
        }

        /// <summary>
        /// UPDATE PROFILE - Change display name, bio, or avatar
        /// Only updates fields that are provided (not null)
        /// </summary>
        public async Task<AuthResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Only update fields that were provided
                if (!string.IsNullOrEmpty(request.DisplayName))
                    user.DisplayName = request.DisplayName;
                
                if (request.Bio != null)
                    user.Bio = request.Bio;
                
                if (!string.IsNullOrEmpty(request.AvatarUrl))
                    user.AvatarUrl = request.AvatarUrl;

                await _userRepository.UpdateAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Profile updated successfully",
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Profile update failed for user {UserId}", userId);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Profile update failed"
                };
            }
        }

        /// <summary>
        /// CHANGE PASSWORD - Update user's password
        /// Requires current password for security
        /// Not allowed for Google OAuth users (they use Google to change password)
        /// </summary>
        public async Task<AuthResponse> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                // Google users don't have passwords
                if (string.IsNullOrEmpty(user.PasswordHash))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Google accounts use Google login to change password"
                    };
                }

                // Verify current password
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash)) // ✅ FIXED
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Current password is incorrect"
                    };
                }

                // Hash and save new password
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword); // ✅ FIXED
                await _userRepository.UpdateAsync(user);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Password changed successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Password change failed for user {UserId}", userId);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Password change failed"
                };
            }
        }

        /// <summary>
        ///  DEACTIVATE ACCOUNT - Soft delete (set IsActive = false)
        /// User can't log in, but data is preserved
        /// Can be reactivated later if needed
        /// </summary>
        public async Task<AuthResponse> DeactivateAccountAsync(Guid userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "User not found"
                    };
                }

                user.IsActive = false;
                user.IsOnline = false;
                user.LastSeen = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User account deactivated: {Username}", user.Username);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Account deactivated successfully"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account deactivation failed for user {UserId}", userId);
                return new AuthResponse
                {
                    Success = false,
                    Message = "Account deactivation failed"
                };
            }
        }

        /// <summary>
        /// REACTIVATE ACCOUNT - Re-enable a deactivated account
        /// User has already proven their identity via correct password in Login
        /// We just flip IsActive back to true and log them in
        /// </summary>
        public async Task<AuthResponse> ReactivateAccountAsync(string usernameOrEmail)
        {
            try
            {
                var user = await _userRepository.GetByUsernameOrEmailAsync(usernameOrEmail);
                if (user == null)
                {
                    return new AuthResponse { Success = false, Message = "User not found" };
                }

                // Reactivate and log in
                user.IsActive = true;
                user.IsOnline = true;
                user.LastSeen = DateTime.UtcNow;
                await _userRepository.UpdateAsync(user);

                var token = _jwtHelper.GenerateToken(user);

                _logger.LogInformation("User account reactivated: {Username}", user.Username);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Account reactivated! Welcome back! 🎉",
                    Token = token,
                    User = MapToUserDto(user)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Account reactivation failed for {UsernameOrEmail}", usernameOrEmail);
                return new AuthResponse { Success = false, Message = "Reactivation failed. Please try again." };
            }
        }

        /// <summary>
        ///  SEARCH USERS - Find users by keyword
        /// Searches in username, display name, and email
        /// Only returns active users (IsActive = true)
        /// </summary>
        public async Task<UserSearchResponse> SearchUsersAsync(string searchTerm)
        {
            try
            {
                var users = await _userRepository.SearchUsersAsync(searchTerm ?? "");
                
                return new UserSearchResponse
                {
                    Users = users.Select(MapToUserDto).ToList(),
                    TotalCount = users.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "User search failed for term: {SearchTerm}", searchTerm);
                return new UserSearchResponse
                {
                    Users = new List<UserDto>(),
                    TotalCount = 0
                };
            }
        }

        /// <summary>
        ///  GET USER BY ID - Retrieve a single user's public info
        /// </summary>
        public async Task<UserDto?> GetUserByIdAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            return user != null ? MapToUserDto(user) : null;
        }

        public async Task<List<UserDto>> GetAllUsersAsync()
        {
            var users = await _userRepository.GetAllUsersAsync();
            return users.Select(MapToUserDto).ToList();
        }

        public async Task<AuthResponse> ToggleUserStatusAsync(Guid userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return new AuthResponse { Success = false, Message = "User not found" };

            user.IsActive = !user.IsActive;
            await _userRepository.UpdateAsync(user);

            return new AuthResponse { Success = true, Message = $"User {(user.IsActive ? "activated" : "deactivated")}" };
        }

        public async Task<List<UserDto>> GetUsersByIdsAsync(List<Guid> userIds)
        {
            var result = new List<UserDto>();
            foreach (var id in userIds)
            {
                var user = await _userRepository.GetByIdAsync(id);
                if (user != null) result.Add(MapToUserDto(user));
            }
            return result;
        }

        /// <summary>
        /// MAPPER - Convert User entity to UserDto (safe for API)
        /// Removes sensitive data like PasswordHash and GoogleId
        /// </summary>
        private UserDto MapToUserDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Bio = user.Bio,
                AvatarUrl = user.AvatarUrl,
                IsOnline = user.IsOnline,
                IsActive = user.IsActive,
                LastSeen = user.LastSeen,
                CreatedAt = user.CreatedAt,
                Role = user.Role
            };
        }
    }
}