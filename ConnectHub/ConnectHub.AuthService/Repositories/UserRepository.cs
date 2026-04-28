using Microsoft.EntityFrameworkCore;
using ConnectHub.AuthService.Models;
using ConnectHub.AuthService.Data;
using ConnectHub.AuthService.Interfaces;

namespace ConnectHub.AuthService.Repositories
{
    /// <summary>
    /// USER REPOSITORY - The actual database operations
    /// This class does the REAL work of talking to PostgreSQL
    /// Implements the IUserRepository interface (the contract)
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get user by their unique ID (fastest - uses primary key)
        /// </summary>
        public async Task<User?> GetByIdAsync(Guid userId)
        {
            return await _context.Users.FindAsync(userId);
        }

        /// <summary>
        /// Get user by username (exact match)
        /// </summary>
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        /// <summary>
        /// Get user by email (exact match)
        /// </summary>
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        /// <summary>
        ///  Get user by username OR email (for login)
        /// </summary>
        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == usernameOrEmail || u.Email == usernameOrEmail);
        }

        /// <summary>
        /// Get user by Google ID (for OAuth users)
        /// </summary>
        public async Task<User?> GetByGoogleIdAsync(string googleId)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.GoogleId == googleId);
        }

        /// <summary>
        /// Create a new user (INSERT into database)
        /// </summary>
        public async Task<User> CreateAsync(User user)
        {
            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            return user;
        }

        /// <summary>
        ///  Update an existing user (UPDATE in database)
        /// </summary>
        public async Task<User> UpdateAsync(User user)
        {
            _context.Users.Update(user);  
            await _context.SaveChangesAsync(); 
            return user;
        }

        /// <summary>
        /// Permanently delete a user (HARD DELETE - rarely used)
        /// </summary>
        public async Task<bool> DeleteAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            if (user == null) return false;
            
            _context.Users.Remove(user);  
            await _context.SaveChangesAsync();  
            return true;
        }

        /// <summary>
        /// Search users by keyword (partial matching)
        /// </summary>
        public async Task<List<User>> SearchUsersAsync(string searchTerm, int limit = 20)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return await _context.Users.Where(u => u.IsActive).OrderBy(u => u.DisplayName).Take(limit).ToListAsync();

            var lowerTerm = searchTerm.ToLower();
            return await _context.Users
                .Where(u => u.IsActive && (
                    u.Username.ToLower().Contains(lowerTerm) || 
                    u.DisplayName.ToLower().Contains(lowerTerm) || 
                    u.Email.ToLower().Contains(lowerTerm)))
                .OrderBy(u => u.DisplayName)
                .Take(limit)
                .ToListAsync();
        }

        /// <summary>
        /// Get all users (Admin only)
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _context.Users.OrderBy(u => u.DisplayName).ToListAsync();
        }

        /// <summary>
        /// Check if username is already taken
        /// </summary>
        public async Task<bool> UsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.Username == username);
        }

        /// <summary>
        ///  Check if email is already registered
        /// </summary>
        public async Task<bool> EmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }
    }
}