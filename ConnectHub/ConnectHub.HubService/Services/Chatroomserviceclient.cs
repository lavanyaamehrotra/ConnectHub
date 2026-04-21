using System.Text;
using System.Text.Json;
using ConnectHub.HubService.Interfaces;

namespace ConnectHub.HubService.Services
{
    // ============================================================
    // HTTP client wrapper for ChatRoomService REST API.
    // Injected into ChatHub as IChatRoomService.
    // ============================================================
    public class ChatRoomServiceClient : IChatRoomService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<ChatRoomServiceClient> _logger;

        public ChatRoomServiceClient(IHttpClientFactory httpClientFactory, ILogger<ChatRoomServiceClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<object> SendRoomMessageAsync(Guid senderId, Guid roomId, string content, string? token = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ChatRoomService");
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { Content = content });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"/api/chatrooms/{roomId}/messages", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? BuildFallback(senderId, roomId, content);
                }

                _logger.LogWarning("ChatRoomService returned {StatusCode} for SendRoomMessage", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.SendRoomMessage failed: {Msg}", ex.Message);
            }

            return BuildFallback(senderId, roomId, content);
        }

        /// <inheritdoc />
        public async Task<List<Guid>> GetUserRoomIdsAsync(string? token = null)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("ChatRoomService");
                ApplyToken(client, token);

                var response = await client.GetAsync("/api/chatrooms");
                if (!response.IsSuccessStatusCode) return new List<Guid>();

                var json = await response.Content.ReadAsStringAsync();
                var rooms = JsonSerializer.Deserialize<List<RoomDto>>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                return rooms?.Select(r => r.RoomId).ToList() ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.GetUserRooms failed: {Msg}", ex.Message);
                return new List<Guid>();
            }
        }

        // ── helpers ──────────────────────────────────────────────
        private static void ApplyToken(HttpClient client, string? token)
        {
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private static object BuildFallback(Guid senderId, Guid roomId, string content) => new
        {
            MessageId = Guid.NewGuid(),
            SenderId  = senderId,
            RoomId    = roomId,
            Content   = content,
            SentAt    = DateTime.UtcNow
        };

        // DTO for deserialising room list from ChatRoomService
        private sealed class RoomDto
        {
            public Guid RoomId { get; set; }
        }
    }
}