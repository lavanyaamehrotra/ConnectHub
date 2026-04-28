using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ConnectHub.HubService.Interfaces;

namespace ConnectHub.HubService.Services
{
    // ============================================================
    // HTTP client wrapper for ChatRoomService REST API.
    // Injected into ChatHub as IChatRoomService.
    // ============================================================
    public class ChatRoomServiceClient : IChatRoomService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ChatRoomServiceClient> _logger;

        public ChatRoomServiceClient(HttpClient httpClient, ILogger<ChatRoomServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<object> SendRoomMessageAsync(Guid senderId, Guid roomId, string content, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { Content = content });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"/api/rooms/{roomId}/messages", httpContent);
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
        public async Task<object> SendRoomMediaMessageAsync(Guid senderId, Guid roomId, string content, string mediaUrl, string messageType, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { Content = content, MediaUrl = mediaUrl, MessageType = messageType });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PostAsync($"/api/rooms/{roomId}/messages", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<object>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? BuildFallback(senderId, roomId, content, mediaUrl, messageType);
                }

                _logger.LogWarning("ChatRoomService returned {StatusCode} for SendRoomMediaMessage", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.SendRoomMediaMessage failed: {Msg}", ex.Message);
            }

            return BuildFallback(senderId, roomId, content, mediaUrl, messageType);
        }

        /// <inheritdoc />
        public async Task<List<Guid>> GetUserRoomIdsAsync(string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var response = await client.GetAsync("/api/rooms/by-user");
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

        public async Task<object> UpdateRoomMessageAsync(Guid userId, Guid messageId, string newContent, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var payload = JsonSerializer.Serialize(new { Content = newContent });
                var httpContent = new StringContent(payload, Encoding.UTF8, "application/json");

                var response = await client.PutAsync($"/api/rooms/messages/{messageId}", httpContent);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<RoomMessageDto>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                        ?? (object)new { MessageId = messageId, Content = newContent, SenderId = userId };
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.UpdateRoomMessage failed: {Msg}", ex.Message);
            }
            return new { MessageId = messageId, Content = newContent, SenderId = userId };
        }

        public async Task<Guid?> DeleteRoomMessageAsync(Guid userId, Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var response = await client.DeleteAsync($"/api/rooms/messages/{messageId}");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<DeleteResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.RoomId;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.DeleteRoomMessage failed: {Msg}", ex.Message);
            }
            return null;
        }

        public async Task<bool> MarkRoomMessageReadAsync(Guid roomId, Guid messageId, string? token = null)
        {
            try
            {
                var client = _httpClient;
                ApplyToken(client, token);

                var response = await client.PostAsync($"/api/rooms/{roomId}/messages/{messageId}/read", null);
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var res = JsonSerializer.Deserialize<MarkReadResponse>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    return res?.FullyRead ?? false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.MarkRoomMessageRead failed: {Msg}", ex.Message);
            }
            return false;
        }

        // ── helpers ──────────────────────────────────────────────
        private static void ApplyToken(HttpClient client, string? token)
        {
            if (!string.IsNullOrEmpty(token))
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        }

        private static object BuildFallback(Guid senderId, Guid roomId, string content, string? mediaUrl = null, string? messageType = null) => new
        {
            MessageId = Guid.NewGuid(),
            SenderId = senderId,
            RoomId = roomId,
            Content = content,
            MediaUrl = mediaUrl,
            MessageType = messageType ?? "TEXT",
            SentAt = DateTime.UtcNow
        };
    }

    public class MarkReadResponse
    {
        public bool FullyRead { get; set; }
    }

    // DTOs for deserialising from ChatRoomService
    public class RoomDto
    {
        public Guid RoomId { get; set; }
    }

    public class RoomMessageDto
    {
        public Guid MessageId { get; set; }
        public Guid RoomId { get; set; }
        public Guid SenderId { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DeleteResponse
    {
        public bool Success { get; set; }
        public Guid? RoomId { get; set; }
    }
}