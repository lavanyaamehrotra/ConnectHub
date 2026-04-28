using Microsoft.EntityFrameworkCore;
using ConnectHub.ChatRoomService.Data;
using ConnectHub.ChatRoomService.Interfaces;
using ConnectHub.ChatRoomService.Models;

namespace ConnectHub.ChatRoomService.Repositories
{
    public class ChatRoomRepository : IChatRoomRepository
    {
        private readonly ApplicationDbContext _context;

        public ChatRoomRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<ChatRoom?> FindByRoomIdAsync(Guid roomId)
        {
            return await _context.ChatRooms
                .Include(r => r.Members)
                .FirstOrDefaultAsync(r => r.RoomId == roomId);
        }

        public async Task<List<ChatRoom>> FindByCreatedByAsync(Guid userId)
        {
            return await _context.ChatRooms
                .Where(r => r.CreatedBy == userId)
                .Include(r => r.Members)
                .ToListAsync();
        }

        public async Task<ChatRoom?> FindByRoomNameAsync(string roomName)
        {
            return await _context.ChatRooms
                .FirstOrDefaultAsync(r => r.RoomName == roomName);
        }

        public async Task<List<ChatRoom>> FindRoomsByUserIdAsync(Guid userId)
        {
            return await _context.RoomMembers
                .Where(m => m.UserId == userId && m.IsActive)
                .Include(m => m.Room)
                .ThenInclude(r => r.Members)
                .Select(m => m.Room)
                .ToListAsync();
        }

        public async Task<List<RoomMember>> FindMembersByRoomIdAsync(Guid roomId)
        {
            return await _context.RoomMembers
                .Where(m => m.RoomId == roomId && m.IsActive)
                .ToListAsync();
        }

        public async Task<bool> IsUserInRoomAsync(Guid userId, Guid roomId)
        {
            return await _context.RoomMembers
                .AnyAsync(m => m.RoomId == roomId && m.UserId == userId && m.IsActive);
        }

        public async Task<int> CountMembersByRoomIdAsync(Guid roomId)
        {
            return await _context.RoomMembers
                .CountAsync(m => m.RoomId == roomId && m.IsActive);
        }

        public async Task<List<ChatRoom>> FindPublicRoomsAsync()
        {
            return await _context.ChatRooms
                .Where(r => r.RoomType == "PUBLIC" && r.IsActive)
                .Include(r => r.Members)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task AddRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Add(room);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateRoomAsync(ChatRoom room)
        {
            _context.ChatRooms.Update(room);
            await _context.SaveChangesAsync();
        }

        public async Task AddMemberAsync(RoomMember member)
        {
            _context.RoomMembers.Add(member);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMemberAsync(RoomMember member)
        {
            _context.RoomMembers.Update(member);
            await _context.SaveChangesAsync();
        }

        public async Task AddMessageAsync(RoomMessage message)
        {
            _context.RoomMessages.Add(message);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateMessageAsync(RoomMessage message)
        {
            _context.RoomMessages.Update(message);
            await _context.SaveChangesAsync();
        }

        public async Task<RoomMessage?> FindMessageByIdAsync(Guid messageId)
        {
            return await _context.RoomMessages.FirstOrDefaultAsync(m => m.MessageId == messageId);
        }

        public async Task<List<RoomMessage>> GetRoomMessagesAsync(Guid roomId, int skip, int take)
        {
            return await _context.RoomMessages
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }
    }
}
