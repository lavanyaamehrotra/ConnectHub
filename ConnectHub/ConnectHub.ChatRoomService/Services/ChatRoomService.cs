using Microsoft.EntityFrameworkCore;
using ConnectHub.ChatRoomService.Data;
using ConnectHub.ChatRoomService.Models;
using ConnectHub.ChatRoomService.DTOs;
using ConnectHub.ChatRoomService.Interfaces;

namespace ConnectHub.ChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly IChatRoomRepository _repository;
        private readonly ILogger<ChatRoomService> _logger;

        public ChatRoomService(IChatRoomRepository repository, ILogger<ChatRoomService> logger)
        {
            _repository = repository;
            _logger = logger;
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
                IsActive = m.IsActive
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
                IsDeleted = false,
                SentAt = DateTime.UtcNow
            };

            await _repository.AddMessageAsync(message);

            return MapToMessageResponse(message);
        }

        public async Task<List<RoomMessageResponse>> GetRoomMessages(Guid roomId, int page = 1, int pageSize = 50)
        {
            var messages = await _repository.GetRoomMessagesAsync(roomId, (page - 1) * pageSize, pageSize);
            return messages.Select(MapToMessageResponse).ToList();
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

        // ========== MAPPERS ==========

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
                IsDeleted = message.IsDeleted
            };
        }
    }
}