using ConnectHub.MessageService.Models;
using ConnectHub.MessageService.DTOs;
using ConnectHub.MessageService.Interfaces;

namespace ConnectHub.MessageService.Services
{
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _repository;
        private readonly ILogger<MessageService> _logger;

        public MessageService(IMessageRepository repository, ILogger<MessageService> logger)
        {
            _repository = repository;
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

            await _repository.AddMessageAsync(message);
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

            await _repository.AddMessageAsync(message);
            return MapToResponse(message);
        }

        public async Task<MessageResponse> GetMessageByIdAsync(Guid userId, Guid messageId)
        {
            var message = await _repository.FindByMessageIdAsync(messageId);

            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId && message.ReceiverId != userId)
                throw new Exception("You don't have access to this message");

            return MapToResponse(message);
        }

        public async Task<ConversationResponse> GetDirectMessagesAsync(Guid userId, Guid otherUserId)
        {
            var messages = await _repository.FindBySenderAndReceiverAsync(userId, otherUserId);

            return new ConversationResponse
            {
                OtherUserId = otherUserId,
                Messages = messages.Select(MapToResponse).ToList()
            };
        }

        public async Task<ConversationResponse> GetRoomMessagesAsync(Guid userId, Guid roomId)
        {
            var messages = await _repository.FindByRoomIdAsync(roomId);

            return new ConversationResponse
            {
                OtherUserId = roomId, // Using this field for Room ID generically
                Messages = messages.Select(MapToResponse).ToList()
            };
        }

        public async Task<int> GetUnreadCountAsync(Guid userId)
        {
            return await _repository.CountUnreadByReceiverIdAsync(userId);
        }

        public async Task<List<RecentChatResponse>> GetRecentChatsAsync(Guid userId)
        {
            var recentMessages = await _repository.FindRecentMessagesAsync(userId);
            var recentChats = new List<RecentChatResponse>();

            foreach (var message in recentMessages)
            {
                var otherUserId = message.SenderId == userId ? message.ReceiverId!.Value : message.SenderId;
                var unreadMessages = await _repository.FindUnreadByReceiverIdAsync(userId, otherUserId);

                recentChats.Add(new RecentChatResponse
                {
                    UserId = otherUserId,
                    Username = "", // Will be filled by frontend via Auth Service
                    DisplayName = "", // Will be filled by frontend
                    LastMessage = MapToResponse(message),
                    UnreadCount = unreadMessages.Count,
                    IsOnline = false // Will be set by Presence Service
                });
            }

            return recentChats.OrderByDescending(c => c.LastMessage?.SentAt).ToList();
        }

        public async Task<SearchMessagesResponse> SearchMessagesAsync(Guid userId, string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return new SearchMessagesResponse();

            var messages = await _repository.SearchMessagesAsync(userId, searchTerm);

            return new SearchMessagesResponse
            {
                Messages = messages.Select(MapToResponse).ToList(),
                TotalCount = messages.Count
            };
        }

        public async Task<MessageResponse> MarkAsReadAsync(Guid userId, Guid messageId)
        {
            var message = await _repository.FindByMessageIdAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.ReceiverId != userId)
                throw new Exception("Only the receiver can mark message as read");

            if (message.IsRead)
                return MapToResponse(message);

            message.IsRead = true;
            message.ReadAt = DateTime.UtcNow;
            
            await _repository.UpdateMessageAsync(message);

            return MapToResponse(message);
        }

        public async Task<int> MarkAllAsReadAsync(Guid userId, Guid otherUserId)
        {
            var unreadMessages = await _repository.FindUnreadByReceiverIdAsync(userId, otherUserId);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _repository.UpdateMessageAsync(message);
            }

            return unreadMessages.Count;
        }

        public async Task<int> MarkAllGlobalAsReadAsync(Guid userId)
        {
            var unreadMessages = await _repository.FindAllUnreadByReceiverIdAsync(userId);

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _repository.UpdateMessageAsync(message);
            }

            return unreadMessages.Count;
        }

        public async Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequest request)
        {
            var message = await _repository.FindByMessageIdAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId)
                throw new Exception("You can only edit your own messages");

            if (message.IsDeleted)
                throw new Exception("Cannot edit a deleted message");

            message.Content = request.Content;
            message.IsEdited = true;
            message.EditedAt = DateTime.UtcNow;

            await _repository.UpdateMessageAsync(message);

            return MapToResponse(message);
        }

        public async Task<bool> DeleteMessageAsync(Guid userId, Guid messageId)
        {
            var message = await _repository.FindByMessageIdAsync(messageId);
            
            if (message == null)
                throw new Exception("Message not found");

            if (message.SenderId != userId && message.ReceiverId != userId)
                throw new Exception("You can only delete messages you sent or received");

            if (message.IsDeleted)
                throw new Exception("Message already deleted");

            return await _repository.DeleteByMessageIdAsync(messageId);
        }

        private static MessageResponse MapToResponse(Message message)
        {
            return new MessageResponse
            {
                MessageId = message.MessageId,
                SenderId = message.SenderId,
                ReceiverId = message.ReceiverId ?? Guid.Empty, // Return Empty if Room Message for DTO compatibility
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