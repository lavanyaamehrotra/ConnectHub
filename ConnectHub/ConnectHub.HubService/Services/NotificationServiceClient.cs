using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ConnectHub.HubService.Interfaces;
using Microsoft.Extensions.Logging;

namespace ConnectHub.HubService.Services
{
    public class NotificationServiceClient : INotificationServiceClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<NotificationServiceClient> _logger;

        public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task SendNotificationAsync(Guid recipientId, Guid senderId, string type, string title, string message, string? relatedId = null, string? relatedType = null, string? token = null)
        {
            try
            {
                if (!string.IsNullOrEmpty(token))
                {
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                _logger.LogInformation("Calling NotificationService for Recipient {RecipientId}...", recipientId);
                var response = await _httpClient.PostAsJsonAsync("/api/notifications/send", new
                {
                    RecipientId = recipientId,
                    SenderId = senderId,
                    Type = type,
                    Title = title,
                    Message = message,
                    RelatedId = relatedId,
                    RelatedType = relatedType
                });

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Notification successfully sent to NotificationService for {RecipientId}", recipientId);
                }
                else
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to send notification to NotificationService: {Status} - {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling NotificationService");
            }
        }
    }
}
