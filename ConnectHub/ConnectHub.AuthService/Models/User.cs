using System;
using System.ComponentModel.DataAnnotations;

namespace ConnectHub.AuthService.Models
{
    /// <summary>
    /// USER MODEL - This represents a person who uses our chat app
    /// Think of this as a form that every user fills out when they join
    /// Each property below becomes a column in the database table "Users"
    /// </summary>
    public class User
    {
        /// <summary>
        /// PRIMARY KEY - Like a government ID number but for our app
        /// Each user gets a unique ID that cannot be guessed (unlike 1,2,3)
        /// GUID stands for Globally Unique Identifier - it's random and secure
        /// </summary>
        [Key]
        public Guid UserId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// USERNAME - What you type to log in
        /// Also used for @mentions in chat like "@john_doe"
        /// Must be unique - no two people can have the same username
        /// Example: "john_doe", "jane_smith_2024"
        /// </summary>
        [Required(ErrorMessage = "Username is required")]
        [MaxLength(50, ErrorMessage = "Username cannot exceed 50 characters")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// EMAIL - Used for login and to send notifications
        /// Must be unique - one email = one account
        /// Example: "john@example.com"
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [MaxLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
        [EmailAddress(ErrorMessage = "Please provide a valid email address")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// PASSWORD HASH - The scrambled version of your password
        /// We NEVER store your actual password for security reasons!
        /// If someone steals our database, they only see scrambled text
        /// NULL for users who sign in with Google (they don't have a password)
        /// </summary>
        [MaxLength(255)]
        public string? PasswordHash { get; set; }

        /// <summary>
        /// DISPLAY NAME - What other users see in chat
        /// Can be different from username (more flexible)
        /// Example: Username "john_doe" but DisplayName "Johnny"
        /// </summary>
        [Required(ErrorMessage = "Display name is required")]
        [MaxLength(100, ErrorMessage = "Display name cannot exceed 100 characters")]
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// BIO - A short description about yourself
        /// Shows up on your profile page
        /// Optional - can be empty or null
        /// Example: "Software developer who loves coffee "
        /// </summary>
        [MaxLength(500, ErrorMessage = "Bio cannot exceed 500 characters")]
        public string? Bio { get; set; }

        /// <summary>
        /// AVATAR URL - Link to your profile picture
        /// If you sign in with Google, we get this automatically
        /// Otherwise you can upload one later
        /// Example: "https://example.com/avatars/john.jpg"
        /// </summary>
        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        /// <summary>
        /// IS ONLINE - Shows if you're currently using the app
        /// true = You're online and ready to chat (green dot)
        /// false = You're offline (gray dot)
        /// Updated automatically when you log in or out
        /// </summary>
        public bool IsOnline { get; set; } = false;

        /// <summary>
        /// LAST SEEN - When you were last active
        /// Shows "Last seen 5 minutes ago" in chat
        /// Updated on login, logout, and when you close the app
        /// </summary>
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IS ACTIVE - Soft delete flag
        /// true = Account is active and can log in
        /// false = Account is deactivated (like deleted but data kept)
        /// When you "deactivate", we set this to false instead of deleting
        /// This way you can reactivate later and keep your data
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// CREATED AT - When you first joined the app
        /// Shows "Member since January 2024" on your profile
        /// Automatically set to current UTC time when you register
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// GOOGLE ID - Google's unique identifier for OAuth users
        /// NULL for users who registered with email/password
        /// Used to connect your Google account to our app
        /// Example: "123456789012345678901"
        /// </summary>
        [MaxLength(255)]
        public string? GoogleId { get; set; }
    }
}