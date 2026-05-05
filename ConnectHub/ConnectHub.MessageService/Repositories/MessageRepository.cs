using Microsoft.EntityFrameworkCore;
using ConnectHub.MessageService.Data;
using ConnectHub.MessageService.Interfaces;
using ConnectHub.MessageService.Models;

namespace ConnectHub.MessageService.Repositories
{
    public class MessageRepository : IMessageRepository
    {
        private readonly ApplicationDbContext _context;

        public MessageRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Message?> FindByMessageIdAsync(Guid messageId)
        {
            return await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);
        }

        public async Task<List<Message>> FindBySenderAndReceiverAsync(Guid userId1, Guid userId2)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted)
                .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                           (m.SenderId == userId2 && m.ReceiverId == userId1))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<Message>> FindByRoomIdAsync(Guid roomId)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && m.RoomId == roomId)
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<List<Message>> FindUnreadByReceiverIdAsync(Guid receiverId, Guid senderId)
        {
            return await _context.Messages
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead && !m.IsDeleted)
                .ToListAsync();
        }
        
        public async Task<List<Message>> FindAllUnreadByReceiverIdAsync(Guid receiverId)
        {
            return await _context.Messages
                .Where(m => m.ReceiverId == receiverId && !m.IsRead && !m.IsDeleted)
                .ToListAsync();
        }

        public async Task<List<Message>> FindRecentMessagesAsync(Guid userId)
        {
            var chatPartners = await GetChatPartnersAsync(userId);
            var recentMessages = new List<Message>();

            foreach (var partnerId in chatPartners)
            {
                var lastMessage = await _context.Messages
                    .Where(m => !m.IsDeleted &&
                               ((m.SenderId == userId && m.ReceiverId == partnerId) ||
                                (m.SenderId == partnerId && m.ReceiverId == userId)))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                if (lastMessage != null)
                {
                    recentMessages.Add(lastMessage);
                }
            }

            return recentMessages.OrderByDescending(m => m.SentAt).ToList();
        }

        public async Task<int> CountUnreadByReceiverIdAsync(Guid receiverId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == receiverId && !m.IsRead && !m.IsDeleted);
        }

        public async Task<int> MarkAllReadByRoomIdAsync(Guid roomId, Guid userId)
        {
            // Note: Since this is room based, you might need a different table to track read receipts per user per room.
            // For now, this is a placeholder if room read receipts are needed.
            // In typical scenarios, RoomId is tracked via a UserRoom table for last read message.
            return 0; 
        }

        public async Task<bool> DeleteByMessageIdAsync(Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            if (message == null || message.IsDeleted) return false;

            message.IsDeleted = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Message>> SearchMessagesAsync(Guid userId, string searchTerm)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && (m.SenderId == userId || m.ReceiverId == userId))
                .Where(m => m.Content.Contains(searchTerm))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync();
        }

        public async Task<Message> AddMessageAsync(Message message)
        {
            _context.Messages.Add(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<Message> UpdateMessageAsync(Message message)
        {
            _context.Messages.Update(message);
            await _context.SaveChangesAsync();
            return message;
        }

        public async Task<List<Guid>> GetChatPartnersAsync(Guid userId)
        {
            return await _context.Messages
                .Where(m => !m.IsDeleted && m.ReceiverId != null && (m.SenderId == userId || m.ReceiverId == userId))
                .Select(m => m.SenderId == userId ? m.ReceiverId!.Value : m.SenderId)
                .Distinct()
                .ToListAsync();
        }
    }
}
