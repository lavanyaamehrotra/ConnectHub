namespace ConnectHub.AuthService.Config
{
    /// <summary>
    /// JWT SETTINGS - Configuration for JSON Web Tokens
    /// JWT is like a digital passport that proves who you are
    /// These settings control how tokens are created and validated
    /// </summary>
    public class JwtSettings
    {
        /// <summary>
        /// SECRET KEY - The "password" used to sign JWT tokens
        /// This must be kept SECRET! Never share it or commit to GitHub!
        /// Must be at least 32 characters long for security
        /// Think of it like the key to a lock - only your server should have it
        /// </summary>
        public string Secret { get; set; } = string.Empty;
        
        /// <summary>
        ///EXPIRATION - How many minutes until the token expires
        /// After 60 minutes, user must log in again
        /// Short expiration is more secure (limits damage if token is stolen)
        /// </summary>
        public int ExpirationInMinutes { get; set; }
        
        /// <summary>
        ///HELPER METHOD - Converts secret string to byte array
        /// JWT libraries require the key as bytes, not as a string
        /// UTF8 is the standard encoding for text to bytes conversion
        /// </summary>
        public byte[] GetSecretBytes() => System.Text.Encoding.UTF8.GetBytes(Secret);
    }
}