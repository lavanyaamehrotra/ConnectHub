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
                CreatedBy = userId,
                IsPrivate = request.IsPrivate,
                CreatedAt = DateTime.UtcNow
            };

            _context.ChatRooms.Add(room);

            var member = new RoomMember
            {
                RoomId = room.RoomId,
                UserId = userId,
                Role = "Admin",
                JoinedAt = DateTime.UtcNow
            };
            _context.RoomMembers.Add(member);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Room {RoomName} created by {UserId}", room.Name, userId);

            return MapToRoomResponse(room, _context);
        }

        public async Task<ChatRoomResponse> UpdateRoomAsync(Guid userId, Guid roomId, UpdateRoomRequest request)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.Role == "Admin");

            if (!isAdmin)
                throw new Exception("Only admin can update room");

            room.Name = request.Name;
            await _context.SaveChangesAsync();

            return MapToRoomResponse(room, _context);
        }

        public async Task<bool> DeleteRoomAsync(Guid userId, Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.Role == "Admin");

            if (!isAdmin)
                throw new Exception("Only admin can delete room");

            _context.ChatRooms.Remove(room);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Room {RoomId} deleted by {UserId}", roomId, userId);

            return true;
        }

        public async Task<List<ChatRoomResponse>> GetUserRoomsAsync(Guid userId)
        {
            // Fetch rooms first, then map in memory (avoids EF projection issue)
            var rooms = await _context.RoomMembers
                .Where(m => m.UserId == userId)
                .Include(m => m.Room)
                .Select(m => m.Room)
                .ToListAsync();

            return rooms.Select(r => MapToRoomResponse(r, _context)).ToList();
        }

        public async Task<ChatRoomResponse> GetRoomAsync(Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            return MapToRoomResponse(room, _context);
        }

        // ========== MEMBER MANAGEMENT ==========

        public async Task<bool> JoinRoomAsync(Guid userId, Guid roomId)
        {
            var room = await _context.ChatRooms.FindAsync(roomId);
            if (room == null)
                throw new Exception("Room not found");

            var existing = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId);

            if (existing)
                throw new Exception("Already a member of this room");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = userId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _context.RoomMembers.Add(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> LeaveRoomAsync(Guid userId, Guid roomId)
        {
            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == userId);

            if (member == null)
                throw new Exception("Not a member of this room");

            if (member.Role == "Admin")
            {
                var otherAdmin = await _context.RoomMembers
                    .Where(m => m.RoomId == roomId && m.UserId != userId)
                    .FirstOrDefaultAsync();

                if (otherAdmin != null)
                {
                    otherAdmin.Role = "Admin";
                }
            }

            _context.RoomMembers.Remove(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<MemberResponse>> GetRoomMembersAsync(Guid roomId)
        {
            var members = await _context.RoomMembers
                .Where(m => m.RoomId == roomId)
                .Select(m => new MemberResponse
                {
                    UserId = m.UserId,
                    Role = m.Role,
                    JoinedAt = m.JoinedAt
                })
                .ToListAsync();

            return members;
        }

        public async Task<bool> AddMemberAsync(Guid adminUserId, Guid roomId, AddMemberRequest request)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "Admin");

            if (!isAdmin)
                throw new Exception("Only admin can add members");

            var existing = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == request.UserId);

            if (existing)
                throw new Exception("User is already a member");

            var member = new RoomMember
            {
                RoomId = roomId,
                UserId = request.UserId,
                Role = "Member",
                JoinedAt = DateTime.UtcNow
            };
            _context.RoomMembers.Add(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "Admin");

            if (!isAdmin)
                throw new Exception("Only admin can remove members");

            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == memberUserId);

            if (member == null)
                throw new Exception("Member not found");

            _context.RoomMembers.Remove(member);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> MakeAdminAsync(Guid adminUserId, Guid roomId, MakeAdminRequest request)
        {
            var isAdmin = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == adminUserId && m.Role == "Admin");

            if (!isAdmin)
                throw new Exception("Only admin can make other admins");

            var member = await _context.RoomMembers
                .FirstOrDefaultAsync(m => m.RoomId == roomId && m.UserId == request.UserId);

            if (member == null)
                throw new Exception("Member not found");

            member.Role = "Admin";
            await _context.SaveChangesAsync();

            return true;
        }

        // ========== MESSAGING ==========

        public async Task<RoomMessageResponse> SendMessageAsync(Guid userId, Guid roomId, SendRoomMessageRequest request)
        {
            var isMember = await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId);

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
            // Fetch first, then map in memory (avoids EF projection issue)
            var messages = await _context.RoomMessages
                .Where(m => m.RoomId == roomId && !m.IsDeleted)
                .OrderByDescending(m => m.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return messages.Select(m => MapToMessageResponse(m)).ToList();
        }

        // ========== MAPPERS ==========

        private static ChatRoomResponse MapToRoomResponse(ChatRoom room, ApplicationDbContext context)
        {
            var memberCount = context.RoomMembers.Count(m => m.RoomId == room.RoomId);

            return new ChatRoomResponse
            {
                RoomId = room.RoomId,
                Name = room.Name,
                CreatedBy = room.CreatedBy,
                IsPrivate = room.IsPrivate,
                CreatedAt = room.CreatedAt,
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