using ConnectHub.MessageService.DTOs;

namespace ConnectHub.MessageService.Interfaces
{
    public interface IMessageService
    {
        Task<MessageResponse> SendMessageAsync(Guid senderId, SendMessageRequest request);
        Task<MessageResponse> SendMediaMessageAsync(Guid senderId, SendMediaMessageRequest request);
        Task<MessageResponse> GetMessageByIdAsync(Guid userId, Guid messageId);
        Task<ConversationResponse> GetDirectMessagesAsync(Guid userId, Guid otherUserId);
        Task<ConversationResponse> GetRoomMessagesAsync(Guid userId, Guid roomId);
        Task<int> GetUnreadCountAsync(Guid userId);
        Task<List<RecentChatResponse>> GetRecentChatsAsync(Guid userId);
        Task<SearchMessagesResponse> SearchMessagesAsync(Guid userId, string searchTerm);
        Task<MessageResponse> MarkAsReadAsync(Guid userId, Guid messageId);
        Task<int> MarkAllAsReadAsync(Guid userId, Guid otherUserId);
        Task<int> MarkAllGlobalAsReadAsync(Guid userId);
        Task<MessageResponse> EditMessageAsync(Guid userId, Guid messageId, EditMessageRequest request);
        Task<bool> DeleteMessageAsync(Guid userId, Guid messageId);
    }
}