using Microsoft.EntityFrameworkCore;
using ConnectHub.MessageService.Data;
using ConnectHub.MessageService.Models;
using ConnectHub.MessageService.DTOs;
using ConnectHub.MessageService.Interfaces;

namespace ConnectHub.MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MessageService> _logger;

        public MessageService(ApplicationDbContext context, ILogger<MessageService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MessageResponse> SendMessageAsync(Guid senderId, SendMessageRequest request)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content,
                MessageType = "TEXT",
                IsRead = false,
                IsEdited = false,
                IsDeleted = false,
                SentAt = DateTime.UtcNow,
                ReadAt = null
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return MapToResponse(message);
        }

        public async Task<MessageResponse> SendMediaMessageAsync(Guid senderId, SendMediaMessageRequest request)
        {
            var message = new Message
            {
                MessageId = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = request.ReceiverId,
                Content = request.Content ?? "",
                MessageType = request.MessageType,
                MediaUrl = request.MediaUrl,
                ReplyToMessageId = request.ReplyToMessageId,
                IsRead = false,
                IsEdited = false,
                IsDeleted = false,
                SentAt = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return MapToResponse(message);
        }

        public async Task<ConversationResponse> GetConversationAsync(Guid userId, Guid otherUserId)
        {
            var messages = await _context.Messages
                .Where(m => !m.IsDeleted)
                .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                           (m.SenderId == otherUserId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => MapToResponse(m))
                .ToListAsync();

            return new ConversationResponse
            {
                OtherUserId = otherUserId,
                Messages = messages
            };
        }

        public async Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequest request)
        {
            var message = await _context.Messages.FindAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId)
                throw new Exception("You can only edit your own messages");

            if (message.IsDeleted)
                throw new Exception("Cannot edit a deleted message");

            message.Content = request.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return MapToResponse(message);
        }

        public async Task<bool> DeleteMessageAsync(Guid userId, Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId && message.ReceiverId != userId)
                throw new Exception("You can only delete messages you sent or received");

            if (message.IsDeleted)
                throw new Exception("Message already deleted");

            message.IsDeleted = true;
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<MessageResponse> MarkAsReadAsync(Guid userId, Guid messageId)
        {
            var message = await _context.Messages.FindAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.ReceiverId != userId)
                throw new Exception("Only the receiver can mark message as read");

            if (message.IsRead)
                return MapToResponse(message);

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return MapToResponse(message);
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId, Guid otherUserId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead && !m.IsDeleted)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return unreadMessages.Count;
        }

        public async Task<SearchMessagesResponse> SearchMessagesAsync(Guid userId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new SearchMessagesResponse();

            var messages = await _context.Messages
                .Where(m => !m.IsDeleted)
                .Where(m => m.SenderId == userId || m.ReceiverId == userId)
                .Where(m => m.Content.Contains(searchTerm))
                .OrderByDescending(m => m.SentAt)
                .Select(m => MapToResponse(m))
                .ToListAsync();

            return new SearchMessagesResponse
            {
                Messages = messages,
                TotalCount = messages.Count
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _context.Messages
                .CountAsync(m => m.ReceiverId == userId && !m.IsRead && !m.IsDeleted);
        }

        public async Task<List<RecentChatResponse>> GetRecentChatsAsync(Guid userId)
        {
            // Get all unique users that the current user has chatted with
            var chatUsers = await _context.Messages
                .Where(m => (m.SenderId == userId || m.ReceiverId == userId) && !m.IsDeleted)
                .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var recentChats = new List<RecentChatResponse>();

            foreach (var otherUserId in chatUsers)
            {
                // Get last message
                var lastMessage = await _context.Messages
                    .Where(m => ((m.SenderId == userId && m.ReceiverId == otherUserId) ||
                                (m.SenderId == otherUserId && m.ReceiverId == userId)) && !m.IsDeleted)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                // Get unread count
                var unreadCount = await _context.Messages
                    .CountAsync(m => m.SenderId == otherUserId && m.ReceiverId == userId && !m.IsRead && !m.IsDeleted);

                if (lastMessage != null)
                {
                    recentChats.Add(new RecentChatResponse
                    {
                        UserId = otherUserId,
                        Username = "", // Will be filled by frontend via Auth Service
                        DisplayName = "", // Will be filled by frontend
                        LastMessage = MapToResponse(lastMessage),
                        UnreadCount = unreadCount,
                        IsOnline = false // Will be set by Presence Service
                    });
                }
            }

            return recentChats.OrderByDescending(c => c.LastMessage?.SentAt).ToList();
        }

        public async Task<MessageResponse> GetMessageByIdAsync(Guid userId, Guid messageId)
        {
            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.MessageId == messageId && !m.IsDeleted);

            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId && message.ReceiverId != userId)
                throw new Exception("You don't have access to this message");

            return MapToResponse(message);
        }

        private static MessageResponse MapToResponse(Message message)
        {
            return new MessageResponse
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId,
                Content = message.Content,
                MessageType = message.MessageType,
                MediaUrl = message.MediaUrl,
                IsRead = message.IsRead,
                ReadAt = message.ReadAt,
                IsEdited = message.IsEdited,
                IsDeleted = message.IsDeleted,
                SentAt = message.SentAt,
                EditedAt = message.EditedAt,
                ReplyToMessageId = message.ReplyToMessageId
            };
        }
    }
}