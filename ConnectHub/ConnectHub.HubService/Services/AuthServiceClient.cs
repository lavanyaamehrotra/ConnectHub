using System.Net.Http.Headers;
using ConnectHub.HubService.Interfaces;

namespace ConnectHub.HubService.Services
{
    public class AuthServiceClient : IAuthServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<AuthServiceClient> _logger;

        public AuthServiceClient(HttpClient httpClient, ILogger<AuthServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task UpdatePresenceAsync(Guid userId, bool isOnline, string token)
        {
            try
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                var response = await _httpClient.PostAsync($"/api/user/{userId}/presence?isOnline={isOnline}", null);
                
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Failed to update presence for {UserId} in AuthService. Status: {Code}", userId, response.StatusCode);
                }
                else
                {
                    _logger.LogInformation("Successfully updated presence for {UserId} to {Status}", userId, isOnline);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling AuthService for presence update");
            }
        }
    }
}
