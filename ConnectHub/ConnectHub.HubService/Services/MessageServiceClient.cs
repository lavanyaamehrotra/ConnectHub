using System.Text;
using System.Text.Json;
using ConnectHub.HubService.Interfaces;

namespace ConnectHub.HubService.Services
{
    // ============================================================
    // HTTP client wrapper for MessageService REST API.
    // Injected into ChatHub as IMessageService.
    // Registered as Scoped (HttpClient created per request scope).
    // ============================================================
    public class MessageServiceClient : IMessageService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MessageServiceClient> _logger;

        public MessageServiceClient(IHttpClientFactory httpClientFactory, ILogger<MessageServiceClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<object> SendMessageAsync(Guid senderId, Guid receiverId, string content, string? token = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MessageService");
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { ReceiverId = receiverId, Content = content });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/messages/send", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? BuildFallback(senderId, receiverId, content);
                }

                _logger.LogWarning("MessageService returned {StatusCode} for SendMessage", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MessageService.SendMessage failed: {Msg}", ex.Message);
            }

            // Fallback: broadcast proceeds even if persistence temporarily fails
            return BuildFallback(senderId, receiverId, content);
        }

        /// <inheritdoc />
        public async Task MarkAsReadAsync(Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MessageService");
                ApplyToken(client, token);

                await client.PutAsync($"/api/messages/read/{messageId}", null);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MessageService.MarkAsRead failed: {Msg}", ex.Message);
            }
        }

        // ── helpers ──────────────────────────────────────────────
        private static void ApplyToken(HttpClient client, string? token)
        {
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private static object BuildFallback(Guid senderId, Guid receiverId, string content) => new
        {
            MessageId  = Guid.NewGuid(),
            SenderId   = senderId,
            ReceiverId = receiverId,
            Content    = content,
            MessageType = "TEXT",
            IsRead     = false,
            SentAt     = DateTime.UtcNow
        };
    }
}