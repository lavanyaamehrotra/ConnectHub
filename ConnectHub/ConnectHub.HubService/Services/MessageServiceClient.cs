using System.Text;
using System.Text.Json;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Models;

namespace ConnectHub.HubService.Services
{
    // ============================================================
    // HTTP client wrapper for MessageService REST API.
    // Injected into ChatHub as IMessageService.
    // Registered as Scoped (HttpClient created per request scope).
    // ============================================================
    public class MessageServiceClient : IMessageService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<MessageServiceClient> _logger;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public MessageServiceClient(HttpClient httpClient, ILogger<MessageServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<object?> SendMessageAsync(Guid senderId, Guid receiverId, string content, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { ReceiverId = receiverId, Content = content });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/messages/send", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(json, _jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("MessageService returned {StatusCode} for SendMessage: {Error}", response.StatusCode, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MessageService.SendMessage CRITICAL FAILURE: {Msg}", ex.Message);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<object?> SendMediaMessageAsync(Guid senderId, Guid receiverId, string content, string mediaUrl, string messageType, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new 
                { 
                    ReceiverId = receiverId, 
                    Content = content,
                    MediaUrl = mediaUrl,
                    MessageType = messageType
                });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync("/api/messages/send-media", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(json, _jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("MessageService returned {StatusCode} for SendMediaMessage: {Error}", response.StatusCode, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MessageService.SendMediaMessage CRITICAL FAILURE: {Msg}", ex.Message);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task MarkAsReadAsync(Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);
                var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
                await client.PutAsync($"/api/messages/markRead/{messageId}", emptyContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MessageService.MarkAsRead failed: {Msg}", ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task MarkAllAsReadAsync(Guid userId, Guid otherUserId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);
                var emptyContent = new StringContent("{}", Encoding.UTF8, "application/json");
                await client.PutAsync($"/api/messages/mark-all-read/{otherUserId}", emptyContent);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("MessageService.MarkAllAsRead failed: {Msg}", ex.Message);
            }
        }

        /// <inheritdoc />
        public async Task<MessageHubResponse?> EditMessageAsync(Guid userId, Guid messageId, string newContent, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);
                var payload = JsonSerializer.Serialize(new { Content = newContent });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");
                var response = await client.PutAsync($"/api/messages/edit/{messageId}", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<MessageHubResponse>(json, _jsonOptions);
                }
            }
            catch (Exception ex) { _logger.LogWarning("Edit failed: {Msg}", ex.Message); }
            return null;
        }

        /// <inheritdoc />
        public async Task<bool> DeleteMessageAsync(Guid userId, Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);
                var response = await client.DeleteAsync($"/api/messages/delete/{messageId}");
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex) { _logger.LogWarning("Delete failed: {Msg}", ex.Message); }
            return false;
        }

        /// <inheritdoc />
        public async Task<MessageHubResponse?> GetMessageByIdAsync(Guid userId, Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);
                var response = await client.GetAsync($"/api/messages/{messageId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<MessageHubResponse>(json, _jsonOptions);
                }
            }
            catch (Exception ex) { _logger.LogWarning("GetById failed: {Msg}", ex.Message); }
            return null;
        }

        // ── helpers ──────────────────────────────────────────────
        private static void ApplyToken(HttpClient client, string? token)
        {
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }
    }
}