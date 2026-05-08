namespace ConnectHub.AuthService.Config
{
    /// <summary>
    ///GOOGLE AUTH SETTINGS - Credentials for Google Login
    /// You get these from Google Cloud Console when you create an OAuth app
    /// Think of it as your app's username and password for Google
    /// </summary>
    public class GoogleAuthSettings
    {
        /// <summary>
        /// CLIENT ID - Public identifier for your app (like a username)
        /// This can be shared publicly (it's okay if people see it)
        /// Example: "123456789-abc123.apps.googleusercontent.com"
        /// </summary>
        public string ClientId { get; set; } = string.Empty;
        
        /// <summary>
        /// CLIENT SECRET - Secret key for your app (like a password)
        /// MUST be kept SECRET! Never expose this in frontend code!
        /// Only your backend server should know this
        /// </summary>
        public string ClientSecret { get; set; } = string.Empty;
    }
}