using ConnectHub.MessageService.DTOs;

namespace ConnectHub.MessageService.Interfaces
{
    public interface IMessageService
    {
        // Basic messaging
        Task<MessageResponse> SendMessageAsync(Guid senderId, SendMessageRequest request);
        Task<MessageResponse> SendMediaMessageAsync(Guid senderId, SendMediaMessageRequest request);
        Task<ConversationResponse> GetConversationAsync(Guid userId, Guid otherUserId);
        Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequest request);
        Task<bool> DeleteMessageAsync(Guid userId, Guid messageId);
        Task<MessageResponse> MarkAsReadAsync(Guid userId, Guid messageId);
        Task<int> MarkAllAsReadAsync(Guid userId, Guid otherUserId);
        Task<SearchMessagesResponse> SearchMessagesAsync(Guid userId, string searchTerm);

        // New features
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<RecentChatResponse>> GetRecentChatsAsync(Guid userId);
        Task<MessageResponse> GetMessageByIdAsync(Guid userId, Guid messageId);
    }
}