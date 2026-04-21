using Microsoft.EntityFrameworkCore;
using ConnectHub.ChatRoomService.Data;
using ConnectHub.ChatRoomService.Models;
using ConnectHub.ChatRoomService.DTOs;
using ConnectHub.ChatRoomService.Interfaces;

namespace ConnectHub.ChatRoomService.Services
{
    public class ChatRoomService : IChatRoomService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatRoomService> _logger;

        public ChatRoomService(ApplicationDbContext context, ILogger<ChatRoomService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ========== ROOM MANAGEMENT ==========

        public async Task<ChatRoomResponse> CreateRoomAsync(Guid userId, CreateRoomRequest request)
        {
            var room = new ChatRoom
            {
                RoomId = Guid.NewGuid(),
                Name = request.Name,
                Description = request.Description,
                RoomType = request.RoomType,
                AvatarUrl = request.AvatarUrl,
                CreatedBy = userId,
                MaxMembers = request.MaxMembers,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ChatRooms.Add(room);

            var member = new RoomMember
            {
                RoomId = room.RoomId,
                UserId = userId,
                Role = "ADMIN",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.RoomMembers.Add(member);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Room {RoomName} created by {UserId}", room.Name, userId);

            return await MapToRoomResponseAsync(room);
        }

        public async Task<ChatRoomResponse> UpdateRoomAsync(Guid userId, Guid roomId, UpdateRoomRequest request)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can update room");

            if (!string.IsNullOrEmpty(request.Name))
                room.Name = request.Name;

            if (request.Description != null)
                room.Description = request.Description;

            if (request.AvatarUrl != null)
                room.AvatarUrl = request.AvatarUrl;

            if (request.MaxMembers.HasValue)
                room.MaxMembers = request.MaxMembers.Value;

            await _context.SaveChangesAsync();

            return await MapToRoomResponseAsync(room);
        }

        public async Task<bool> DeleteRoomAsync(Guid userId, Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.Role == "ADMIN");

            if (!isAdmin)
                throw new Exception("Only admin can delete room");

            room.IsActive = false;
            await _context.SaveChangesAsync();

            _logger.LogInformation("Room {RoomId} deleted by {UserId}", roomId, userId);

            return true;
        }

        public async Task<List<ChatRoomResponse>> GetUserRoomsAsync(Guid userId)
        {
            var rooms = await _context.RoomMembers
                .Where(m => m.UserId == userId && m.IsActive)
                .Include(m => m.Room)
                .Select(m => m.Room)
                .ToListAsync();

            var responses = new List<ChatRoomResponse>();
            foreach (var room in rooms)
            {
                responses.Add(await MapToRoomResponseAsync(room));
            }
            return responses;
        }

        public async Task<ChatRoomResponse> GetRoomAsync(Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            return await MapToRoomResponseAsync(room);
        }

        public async Task<List<ChatRoomResponse>> GetPublicRoomsAsync()
        {
            var rooms = await _context.ChatRooms
                .Where(r => r.RoomType == "PUBLIC" && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var responses = new List<ChatRoomResponse>();
            foreach (var room in rooms)
            {
                responses.Add(await MapToRoomResponseAsync(room));
            }
            return responses;
        }

        public async Task<bool> IsUserInRoomAsync(Guid userId, Guid roomId)
        {
            return await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);
        }

        public async Task<int> GetMemberCountAsync(Guid roomId)
        {
            return await _context.RoomMembers
                .CountAsync(m => m.RoomId == roomId && m.IsActive);
        }

        // ========== MEMBER MANAGEMENT ==========

        public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            // Check if room is private
            if (room.RoomType == "PRIVATE")
                throw new Exception("Cannot join private room. You need an invitation.");

            // Check member limit
            var currentCount = await GetMemberCountAsync(roomId);
            if (currentCount >= room.MaxMembers)
                throw new Exception("Room has reached maximum member limit");

            var existing = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId);

            if (existing)
                throw new Exception("Already a member of this room");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                Role = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.RoomMembers.Add(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> LeaveRoomAsync(Guid userId, Guid roomId)
        {
            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);

            if (member == null)
                throw new Exception("Not a member of this room");

            // If admin is leaving, assign another admin
            if (member.Role == "ADMIN")
            {
                var otherAdmin = await _context.RoomMembers
                    .Where(m => m.RoomId == roomId && m.UserId != userId && m.IsActive)
                    .FirstOrDefaultAsync();

                if (otherAdmin != null)
                {
                    otherAdmin.Role = "ADMIN";
                }
            }

            member.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<MemberResponse>> GetRoomMembersAsync(Guid roomId)
        {
            var members = await _context.RoomMembers
                .Where(m => m.RoomId == roomId && m.IsActive)
                .Select(m => new MemberResponse
                {
                    UserId = m.UserId,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt,
                    IsActive = m.IsActive
                })
                .ToListAsync();

            return members;
        }

        public async Task<bool> AddMemberAsync(Guid adminUserId, Guid roomId, AddMemberRequest request)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "ADMIN" && m.IsActive);

            if (!isAdmin)
                throw new Exception("Only admin can add members");

            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var currentCount = await GetMemberCountAsync(roomId);
            if (currentCount >= room.MaxMembers)
                throw new Exception("Room has reached maximum member limit");

            var existing = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == request.UserId && m.IsActive);

            if (existing)
                throw new Exception("User is already a member");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = request.UserId,
                Role = "MEMBER",
                JoinedAt = DateTime.UtcNow,
                IsActive = true
            };
            _context.RoomMembers.Add(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "ADMIN" && m.IsActive);

            if (!isAdmin)
                throw new Exception("Only admin can remove members");

            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == memberUserId && m.IsActive);

            if (member == null)
                throw new Exception("Member not found");

            member.IsActive = false;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MakeAdminAsync(Guid adminUserId, Guid roomId, MakeAdminRequest request)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "ADMIN" && m.IsActive);

            if (!isAdmin)
                throw new Exception("Only admin can make other admins");

            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == request.UserId && m.IsActive);

            if (member == null)
                throw new Exception("Member not found");

            member.Role = "ADMIN";
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> UpdateMemberRoleAsync(Guid adminUserId, Guid roomId, UpdateMemberRoleRequest request)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "ADMIN" && m.IsActive);

            if (!isAdmin)
                throw new Exception("Only admin can update member roles");

            if (request.Role != "ADMIN" && request.Role != "MODERATOR" && request.Role != "MEMBER")
                throw new Exception("Invalid role. Must be ADMIN, MODERATOR, or MEMBER");

            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == request.UserId && m.IsActive);

            if (member == null)
                throw new Exception("Member not found");

            member.Role = request.Role;
            await _context.SaveChangesAsync();

            return true;
        }

        // ========== MESSAGING ==========

        public async Task<RoomMessageResponse> SendMessageAsync(Guid userId, Guid roomId, SendRoomMessageRequest request)
        {
            var isMember = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);

            if (!isMember)
                throw new Exception("You must be a member to send messages");

            var message = new RoomMessage
            {
                MessageId = Guid.NewGuid(),
                RoomId = roomId,
                SenderId = userId,
                Content = request.Content,
                IsDeleted = false,
                SentAt = DateTime.UtcNow
            };

            _context.RoomMessages.Add(message);
            await _context.SaveChangesAsync();

            return MapToMessageResponse(message);
        }

        public async Task<List<RoomMessageResponse>> GetRoomMessagesAsync(Guid roomId, int page = 1, int pageSize = 50)
        {
            var messages = await _context.RoomMessages
                .Where(m => m.RoomId == roomId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages.Select(MapToMessageResponse).ToList();
        }

        // ========== MAPPERS ==========

        private async Task<ChatRoomResponse> MapToRoomResponseAsync(ChatRoom room)
        {
            var memberCount = await _context.RoomMembers
                .CountAsync(m => m.RoomId == room.RoomId && m.IsActive);

            return new ChatRoomResponse
            {
                RoomId = room.RoomId,
                Name = room.Name,
                Description = room.Description,
                RoomType = room.RoomType,
                AvatarUrl = room.AvatarUrl,
                CreatedBy = room.CreatedBy,
                CreatedAt = room.CreatedAt,
                IsActive = room.IsActive,
                MaxMembers = room.MaxMembers,
                MemberCount = memberCount
            };
        }

        private static RoomMessageResponse MapToMessageResponse(RoomMessage message)
        {
            return new RoomMessageResponse
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                Content = message.Content,
                SentAt = message.SentAt,
                IsDeleted = message.IsDeleted
            };
        }
    }
}