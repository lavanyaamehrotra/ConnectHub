using Microsoft.EntityFrameworkCore;
using ConnectHub.ChatRoomService.Data;
using ConnectHub.ChatRoomService.Models;
using ConnectHub.ChatRoomService.DTOs;
using ConnectHub.ChatRoomService.Interfaces;
using System.Text.Json;

namespace ConnectHub.ChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly IChatRoomRepository _repository;
        private readonly ILogger<ChatRoomService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        public ChatRoomService(IChatRoomRepository repository, ILogger<ChatRoomService> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _repository = repository;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
            _configuration = configuration;
        }

        // ========== ROOM MANAGEMENT ==========

        public async Task<ChatRoomResponse> CreateRoom(Guid userId, string username, CreateRoomRequest request)
        {
            var room = new ChatRoom
            {
                RoomId = Guid.NewGuid(),
                RoomName = request.RoomName,
                Description = request.Description,
                RoomType = request.RoomType,
                AvatarUrl = request.AvatarUrl,
                CreatedBy = userId,
                MaxMembers = request.MaxMembers,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            await _repository.AddRoomAsync(room);

            var member = new RoomMember
            {
                RoomId = room.RoomId,
                UserId = userId,
                Username = username,
                Role = "ADMIN",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _repository.AddMemberAsync(member);

            _logger.LogInformation("Room {RoomName} created by {UserId}", room.RoomName, userId);

            // Notify Hub Service that a new room was added for the creator
            await NotifyRoomAdded(userId, room.RoomId);

            return MapToRoomResponse(room);
        }

        public async Task<ChatRoomResponse> UpdateRoom(Guid userId, Guid roomId, UpdateRoomRequest request)
        {
            var room = await _repository.FindByRoomIdAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var isAdmin = members.Any(m => m.UserId == userId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can update room");

            if (!string.IsNullOrEmpty(request.RoomName))
                room.RoomName = request.RoomName;

            if (request.Description != null)
                room.Description = request.Description;

            if (request.AvatarUrl != null)
                room.AvatarUrl = request.AvatarUrl;

            if (request.MaxMembers.HasValue)
                room.MaxMembers = request.MaxMembers.Value;

            await _repository.UpdateRoomAsync(room);

            return MapToRoomResponse(room);
        }

        public async Task<bool> DeleteRoom(Guid userId, Guid roomId)
        {
            var room = await _repository.FindByRoomIdAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var isAdmin = members.Any(m => m.UserId == userId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can delete room");

            room.IsActive = false;
            await _repository.UpdateRoomAsync(room);

            _logger.LogInformation("Room {RoomId} deleted by {UserId}", roomId, userId);

            return true;
        }

        public async Task<List<ChatRoomResponse>> GetRoomsByUser(Guid userId)
        {
            var rooms = await _repository.FindRoomsByUserIdAsync(userId);
            return rooms.Select(MapToRoomResponse).ToList();
        }

        public async Task<ChatRoomResponse> GetRoomById(Guid roomId)
        {
            var room = await _repository.FindByRoomIdAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            return MapToRoomResponse(room);
        }

        public async Task<List<ChatRoomResponse>> GetPublicRooms()
        {
            var rooms = await _repository.FindPublicRoomsAsync();
            return rooms.Select(MapToRoomResponse).ToList();
        }

        public async Task<bool> IsUserInRoom(Guid userId, Guid roomId)
        {
            return await _repository.IsUserInRoomAsync(userId, roomId);
        }

        public async Task<int> GetMemberCount(Guid roomId)
        {
            return await _repository.CountMembersByRoomIdAsync(roomId);
        }

        // ========== MEMBER MANAGEMENT ==========

        public async Task<bool> JoinRoom(Guid userId, string username, Guid roomId)
        {
            var room = await _repository.FindByRoomIdAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            if (room.RoomType == "PRIVATE")
                throw new Exception("Cannot join private room. You need an invitation.");

            var currentCount = await _repository.CountMembersByRoomIdAsync(roomId);
            if (currentCount >= room.MaxMembers)
                throw new Exception("Room has reached maximum member limit");

            var existing = await _repository.IsUserInRoomAsync(userId, roomId);
            if (existing)
                throw new Exception("Already a member of this room");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                Username = username,
                Role = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _repository.AddMemberAsync(member);

            // Notify Hub Service that a new room was added for the user who joined
            await NotifyRoomAdded(userId, roomId);

            return true;
        }

        public async Task<bool> LeaveRoom(Guid userId, Guid roomId)
        {
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var member = members.FirstOrDefault(m => m.UserId == userId && m.IsActive);

            if (member == null)
                throw new Exception("Not a member of this room");

            if (member.Role == "ADMIN")
            {
                var otherAdmin = members.FirstOrDefault(m => m.UserId != userId && m.IsActive);
                if (otherAdmin != null)
                {
                    otherAdmin.Role = "ADMIN";
                    await _repository.UpdateMemberAsync(otherAdmin);
                }
            }

            member.IsActive = false;
            await _repository.UpdateMemberAsync(member);

            return true;
        }

        public async Task<List<MemberResponse>> GetMembers(Guid roomId)
        {
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            return members.Select(m => new MemberResponse
            {
                UserId = m.UserId,
                Username = m.Username,
                Role = m.Role,
                JoinedAt = m.JoinedAt,
                IsActive = m.IsActive,
                LastReadMessageId = m.LastReadMessageId,
                LastReadAt = m.LastReadAt
            }).ToList();
        }

        public async Task<bool> AddMember(Guid adminUserId, Guid roomId, AddMemberRequest request)
        {
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var isAdmin = members.Any(m => m.UserId == adminUserId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can add members");

            var room = await _repository.FindByRoomIdAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var currentCount = await _repository.CountMembersByRoomIdAsync(roomId);
            if (currentCount >= room.MaxMembers)
                throw new Exception("Room has reached maximum member limit");

            var existing = members.Any(m => m.UserId == request.UserId && m.IsActive);
            if (existing)
                throw new Exception("User is already a member");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = request.UserId,
                Username = request.Username,
                Role = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            await _repository.AddMemberAsync(member);

            // NEW: Notify Hub Service that a new room was added for the invited user
            await NotifyRoomAdded(request.UserId, roomId);

            return true;
        }

        public async Task<bool> RemoveMember(Guid adminUserId, Guid roomId, Guid memberUserId)
        {
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var isAdmin = members.Any(m => m.UserId == adminUserId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can remove members");

            var member = members.FirstOrDefault(m => m.UserId == memberUserId && m.IsActive);
            if (member == null)
                throw new Exception("Member not found");

            member.IsActive = false;
            await _repository.UpdateMemberAsync(member);

            return true;
        }

        public async Task<bool> UpdateMemberRole(Guid adminUserId, Guid roomId, UpdateMemberRoleRequest request)
        {
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var isAdmin = members.Any(m => m.UserId == adminUserId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can update member roles");

            if (request.Role != "ADMIN" && request.Role != "MODERATOR" && request.Role != "MEMBER")
                throw new Exception("Invalid role. Must be ADMIN, MODERATOR, or MEMBER");

            var member = members.FirstOrDefault(m => m.UserId == request.UserId && m.IsActive);
            if (member == null)
                throw new Exception("Member not found");

            member.Role = request.Role;
            await _repository.UpdateMemberAsync(member);

            return true;
        }

        // ========== MESSAGING ==========

        public async Task<RoomMessageResponse> SendMessage(Guid userId, Guid roomId, SendRoomMessageRequest request)
        {
            var isMember = await _repository.IsUserInRoomAsync(userId, roomId);
            if (!isMember)
                throw new Exception("You must be a member to send messages");

            var message = new RoomMessage
            {
                MessageId = Guid.NewGuid(),
                RoomId = roomId,
                SenderId = userId,
                Content = request.Content,
                MediaUrl = request.MediaUrl,
                MessageType = request.MessageType ?? "TEXT",
                SentAt = DateTime.UtcNow,
                IsRead = false
            };
            await _repository.AddMessageAsync(message);

            // NEW: Update sender's last read status immediately
            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var me = members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
            if (me != null)
            {
                me.LastReadMessageId = message.MessageId;
                me.LastReadAt = message.SentAt;
                await _repository.UpdateMemberAsync(me);
            }

            return MapToMessageResponse(message);
        }

        public async Task<List<RoomMessageResponse>> GetRoomMessages(Guid roomId, int page = 1, int pageSize = 50)
        {
            var messages = await _repository.GetRoomMessagesAsync(roomId, (page - 1) * pageSize, pageSize);
            return messages.Select(MapToMessageResponse).ToList();
        }

        public async Task<bool> MarkRoomMessageAsRead(Guid userId, Guid roomId, Guid messageId)
        {
            var message = await _repository.FindMessageByIdAsync(messageId);
            if (message == null || message.RoomId != roomId) return false;

            var members = await _repository.FindMembersByRoomIdAsync(roomId);
            var me = members.FirstOrDefault(m => m.UserId == userId && m.IsActive);
            if (me == null) return false;

            // 1. Update my last read status (idempotent - only move forward)
            if (me.LastReadAt == null || me.LastReadAt < message.SentAt)
            {
                me.LastReadMessageId = messageId;
                me.LastReadAt = DateTime.UtcNow;
                await _repository.UpdateMemberAsync(me);
            }

            // 2. RE-FETCH members to get the absolute latest status from other concurrent readers
            var freshMembers = await _repository.FindMembersByRoomIdAsync(roomId);
            
            // 3. Check if this message is now read by everyone
            // Relaxed check: 5-second buffer and explicitly include current reader
            var activeMembers = freshMembers.Where(m => m.IsActive).ToList();
            var allRead = activeMembers.All(m => m.UserId == message.SenderId || 
                                               m.UserId == userId || 
                                               (m.LastReadAt.HasValue && m.LastReadAt.Value.AddSeconds(5) >= message.SentAt)); 

            _logger.LogInformation("DIAGNOSTIC: Room {RoomId}, Message {MsgId} (SentAt: {SentAt}). " +
                                  "Reader {ReaderId}. AllRead: {AllRead}. " +
                                  "Members: {Details}", 
                                  roomId, messageId, message.SentAt.ToString("O"), userId, allRead,
                                  string.Join(" | ", activeMembers.Select(m => $"{m.Username}({m.UserId}): {(m.LastReadAt.HasValue ? m.LastReadAt.Value.ToString("O") : "null")}")));

            if (allRead && !message.IsRead)
            {
                // MARK ALL PREVIOUS MESSAGES IN THIS ROOM AS READ TOO
                await _repository.MarkMessagesAsReadUntilAsync(roomId, message.SentAt);
                return true; 
            }

            return false;
        }

        public async Task<RoomMessageResponse> UpdateMessage(Guid userId, Guid messageId, string newContent)
        {
            var message = await _repository.FindMessageByIdAsync(messageId);
            if (message == null) throw new Exception("Message not found");
            if (message.SenderId != userId) throw new Exception("Unauthorized to edit this message");

            message.Content = newContent;
            await _repository.UpdateMessageAsync(message);
            return MapToMessageResponse(message);
        }

        public async Task<Guid?> DeleteMessage(Guid userId, Guid messageId)
        {
            var message = await _repository.FindMessageByIdAsync(messageId);
            if (message == null) throw new Exception("Message not found");
            
            // Check if admin of room or sender
            var isSender = message.SenderId == userId;
            var members = await _repository.FindMembersByRoomIdAsync(message.RoomId);
            var isAdmin = members.Any(m => m.UserId == userId && m.Role == "ADMIN");

            if (!isSender && !isAdmin) throw new Exception("Unauthorized to delete this message");

            message.IsDeleted = true;
            await _repository.UpdateMessageAsync(message);
            return message.RoomId;
        }

        // ========== PRIVATE HELPERS ==========

        private async Task NotifyRoomAdded(Guid userId, Guid roomId)
        {
            try
            {
                var hubServiceUrl = _configuration["HUB_SERVICE_URL"] ?? "http://connecthub-hub:5006";
                var endpoint = $"{hubServiceUrl}/api/notify/room-added";
                
                var payload = new { UserId = userId, RoomId = roomId };
                var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Failed to notify Hub Service: {StatusCode} - {Error}", response.StatusCode, error);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error notifying Hub Service about new room");
            }
        }

        private static ChatRoomResponse MapToRoomResponse(ChatRoom room)
        {
            return new ChatRoomResponse
            {
                RoomId = room.RoomId,
                RoomName = room.RoomName,
                Description = room.Description,
                RoomType = room.RoomType,
                AvatarUrl = room.AvatarUrl,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
                IsActive = room.IsActive,
                MaxMembers = room.MaxMembers,
                MemberCount = room.Members?.Count ?? 0
            };
        }

        private static RoomMessageResponse MapToMessageResponse(RoomMessage message)
        {
            return new RoomMessageResponse
            {
                MessageId = message.MessageId,
                RoomId = message.RoomId,
                SenderId = message.SenderId,
                Content = message.Content,
                MediaUrl = message.MediaUrl,
                MessageType = message.MessageType,
                SentAt = message.SentAt,
                IsDeleted = message.IsDeleted,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt
            };
        }
    }
}