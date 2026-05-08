using Google.Apis.Auth;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ConnectHub.AuthService.Config;

namespace ConnectHub.AuthService.Helpers
{
    /// <summary>
    /// GOOGLE AUTH HELPER - Verifies Google login tokens
    /// When user clicks "Sign in with Google", frontend gets an ID token
    /// We send that token to Google to verify it's genuine
    /// Think of it as asking Google "Is this person really who they say they are?"
    /// </summary>
    public class GoogleAuthHelper
    {
        private readonly GoogleAuthSettings _googleSettings;
        private readonly ILogger<GoogleAuthHelper> _logger;

        /// <summary>
        /// Constructor - Receives Google OAuth credentials from appsettings.json
        /// </summary>
        public GoogleAuthHelper(
            IOptions<GoogleAuthSettings> googleSettings,
            ILogger<GoogleAuthHelper> logger)
        {
            _googleSettings = googleSettings.Value;
            _logger = logger;
        }

        /// <summary>
        /// VERIFY GOOGLE TOKEN - Checks if a Google ID token is authentic
        /// This is the most important method for Google login security
        /// We ask Google to validate the token to ensure it's not fake
        /// </summary>
        /// <param name="idToken">Google ID token from frontend</param>
        /// <returns>User payload if valid, null if invalid</returns>
        public virtual async Task<GoogleJsonWebSignature.Payload?> VerifyGoogleToken(string idToken)
        {
            if (string.IsNullOrWhiteSpace(_googleSettings.ClientId))
            {
                _logger.LogError("Google ClientId is not configured. Check appsettings.json Google:ClientId.");
                return null;
            }

            try
            {
                ///  VALIDATION SETTINGS - Tells Google what to check
                var settings = new GoogleJsonWebSignature.ValidationSettings()
                {
                    /// Audience must match our Client ID
                    /// This ensures the token was created for OUR app, not someone else's
                    /// Prevents attackers from using tokens meant for other websites
                    Audience = new[] { _googleSettings.ClientId }
                };

                ///  ASK GOOGLE TO VERIFY - Makes an HTTP call to Google's servers
                /// Google returns user information if token is valid
                /// If token is fake/expired, Google throws an exception
                var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
                
                /// TOKEN IS VALID! Payload contains user info:
                /// - Email: user's email address
                /// - Name: user's full name  
                /// - Picture: URL to profile picture
                /// - Subject: unique Google user ID (stays same forever)
                return payload;
            }
            catch (Exception ex)
            {
                /// TOKEN IS INVALID (expired, fake, or wrong app)
                /// Return null - login should fail
                _logger.LogError(ex, "Google token verification failed: {Message}", ex.Message);
                return null;
            }
        }
    }
}