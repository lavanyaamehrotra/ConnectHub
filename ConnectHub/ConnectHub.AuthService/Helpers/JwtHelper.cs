using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using ConnectHub.AuthService.Models;
using ConnectHub.AuthService.Config;
using Microsoft.Extensions.Options;

namespace ConnectHub.AuthService.Helpers
{
    /// <summary>
    /// JWT HELPER - Creates and validates JSON Web Tokens
    /// JWT is like a digital passport that proves who a user is
    /// It contains user information (claims) and is signed to prevent tampering
    /// Think of it as a tamper-proof ID card that your server issues
    /// </summary>
    public class JwtHelper
    {
        private readonly JwtSettings _jwtSettings;

        /// <summary>
        /// Constructor - Receives JWT settings from appsettings.json
        /// IOptions pattern is how .NET injects configuration values
        /// </summary>
        public JwtHelper(IOptions<JwtSettings> jwtSettings)
        {
            _jwtSettings = jwtSettings.Value;
        }

        /// <summary>
        ///GENERATE TOKEN - Creates a new JWT for a user
        /// Called when user logs in or registers
        /// The token contains user identity and expires after set time
        /// </summary>
        /// <param name="user">The user to generate token for</param>
        /// <returns>A JWT token string that looks like "eyJhbGciOiJIUzI1NiIs..."</returns>
        public virtual string GenerateToken(User user)
        {
            ///  CLAIMS - Information stored inside the token
            /// Think of these as passport information (name, ID, photo)
            /// These are embedded in the token and can be read without database
            var claims = new[]
            {
                /// NameIdentifier - User's unique ID (standard claim type)
                /// Used to identify which user this token belongs to
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                
                /// Name - User's username (standard claim)
                new Claim(ClaimTypes.Name, user.Username),
                
                /// Email - User's email address (standard claim)
                new Claim(ClaimTypes.Email, user.Email),
                
                /// Custom claim - Display name for chat UI
                /// Not a standard claim type, so we create our own
                new Claim("DisplayName", user.DisplayName),

                /// ROLE - Used for authorization (Admin vs User)
                new Claim(ClaimTypes.Role, user.Role)
            };

            /// SIGNING KEY - Used to sign the token (like a wax seal)
            /// Converts our secret string into bytes that crypto algorithms use
            var key = new SymmetricSecurityKey(_jwtSettings.GetSecretBytes());
            
            /// SIGNING CREDENTIALS - Combines key and algorithm
            /// HmacSha256 is a secure hashing algorithm (industry standard)
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            /// CREATE TOKEN - Build the actual JWT token object
            var token = new JwtSecurityToken(
                issuer: null,        // Who issued token (optional for simplicity)
                audience: null,      // Who can use token (optional)
                claims: claims,      // User information to embed
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationInMinutes), // When it dies
                signingCredentials: credentials  // The signature to prevent tampering
            );

            /// Convert token object to string format (so it can be sent in HTTP headers)
            /// The string looks like: "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// VALIDATE TOKEN - Checks if a token is valid
        /// Used when we need to verify a token (e.g., for WebSocket connections)
        /// Returns null if token is invalid, expired, or tampered with
        /// </summary>
        /// <param name="token">JWT token string to validate</param>
        /// <returns>ClaimsPrincipal with user info if valid, null if invalid</returns>
        public virtual ClaimsPrincipal? ValidateToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(_jwtSettings.GetSecretBytes());

            try
            {
                /// Attempt to validate the token
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,   // Check that signature is valid
                    IssuerSigningKey = key,            // Use our secret key to verify
                    ValidateIssuer = false,            // Don't check issuer (simpler setup)
                    ValidateAudience = false,          // Don't check audience (simpler setup)
                    ClockSkew = TimeSpan.Zero          // No time tolerance (strict expiry)
                }, out _);

                return principal; // Token is valid - return user info
            }
            catch
            {
                return null; // Token is invalid (expired, wrong signature, etc.)
            }
        }
    }
}