using ConnectHub.AuthService.Models;

namespace ConnectHub.AuthService.Interfaces
{
    /// <summary>
    /// USER REPOSITORY INTERFACE - The "menu" of database operations
    /// This defines WHAT you can do with users, not HOW it's done
    /// Think of it as a restaurant menu - it lists what you can order
    /// The actual cooking (implementation) is in Repositories/UserRepository.cs
    /// Why separate? Makes it easy to test and swap databases later!
    /// </summary>
    public interface IUserRepository
    {
        // ==========  READ OPERATIONS (Get data) ==========
        
        /// <summary>Find a user by their unique ID</summary>
        Task<User?> GetByIdAsync(Guid userId);
        
        /// <summary>Find a user by their username</summary>
        Task<User?> GetByUsernameAsync(string username);
        
        /// <summary>Find a user by their email address</summary>
        Task<User?> GetByEmailAsync(string email);
        
        /// <summary>Find a user by username OR email (for login)</summary>
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        
        /// <summary>Find a user by their Google ID (for OAuth)</summary>
        Task<User?> GetByGoogleIdAsync(string googleId);
        
        // ==========  WRITE OPERATIONS (Change data) ==========
        
        /// <summary>Add a new user to the database</summary>
        Task<User> CreateAsync(User user);
        
        /// <summary>Update an existing user's information</summary>
        Task<User> UpdateAsync(User user);
        
        /// <summary>Permanently delete a user (rarely used)</summary>
        Task<bool> DeleteAsync(Guid userId);
        
        // ==========  SEARCH OPERATIONS ==========
        
        /// <summary>Search users by name, username, or email</summary>
        Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 20);

        /// <summary>Get all users (Admin only)</summary>
        Task<List<User>> GetAllUsersAsync();
        
        // ==========  VALIDATION OPERATIONS (Check if exists) ==========
        
        /// <summary>Check if a username is already taken</summary>
        Task<bool> UsernameExistsAsync(string username);
        
        /// <summary>Check if an email is already registered</summary>
        Task<bool> EmailExistsAsync(string email);
    }
}