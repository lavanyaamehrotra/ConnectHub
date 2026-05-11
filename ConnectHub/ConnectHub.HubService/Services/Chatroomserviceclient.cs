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
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

        public ChatRoomServiceClient(HttpClient httpClient, ILogger<ChatRoomServiceClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        /// <inheritdoc />
        public async Task<object?> SendRoomMessageAsync(Guid senderId, Guid roomId, string content, string? token = null)
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
                    return JsonSerializer.Deserialize<object>(json, _jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ChatRoomService returned {StatusCode} for SendRoomMessage: {Error}", response.StatusCode, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatRoomService.SendRoomMessage CRITICAL FAILURE: {Msg}", ex.Message);
            }

            return null;
        }

        /// <inheritdoc />
        public async Task<object?> SendRoomMediaMessageAsync(Guid senderId, Guid roomId, string content, string mediaUrl, string messageType, string? token = null)
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
                    return JsonSerializer.Deserialize<object>(json, _jsonOptions);
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("ChatRoomService returned {StatusCode} for SendRoomMediaMessage: {Error}", response.StatusCode, error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ChatRoomService.SendRoomMediaMessage CRITICAL FAILURE: {Msg}", ex.Message);
            }

            return null;
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
                var rooms = JsonSerializer.Deserialize<List<RoomDto>>(json, _jsonOptions);

                return rooms?.Select(r => r.RoomId).ToList() ?? new List<Guid>();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.GetUserRooms failed: {Msg}", ex.Message);
                return new List<Guid>();
            }
        }

        public async Task<object?> UpdateRoomMessageAsync(Guid userId, Guid messageId, string newContent, string? token = null)
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
                    return JsonSerializer.Deserialize<RoomMessageDto>(json, _jsonOptions);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("ChatRoomService.UpdateRoomMessage failed: {Msg}", ex.Message);
            }
            return null;
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
                    var res = JsonSerializer.Deserialize<DeleteResponse>(json, _jsonOptions);
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
                    var res = JsonSerializer.Deserialize<MarkReadResponse>(json, _jsonOptions);
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