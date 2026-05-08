using ConnectHub.MessageService.Models;

namespace ConnectHub.MessageService.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message?> FindByMessageIdAsync(Guid messageId);
        Task<List<Message>> FindBySenderAndReceiverAsync(Guid userId1, Guid userId2);
        Task<List<Message>> FindByRoomIdAsync(Guid roomId);
        Task<List<Message>> FindUnreadByReceiverIdAsync(Guid receiverId, Guid senderId);
        Task<List<Message>> FindAllUnreadByReceiverIdAsync(Guid receiverId);
        Task<List<Message>> FindRecentMessagesAsync(Guid userId);
        Task<int> CountUnreadByReceiverIdAsync(Guid receiverId);
        Task<int> MarkAllReadByRoomIdAsync(Guid roomId, Guid userId);
        Task<bool> DeleteByMessageIdAsync(Guid messageId);
        Task<List<Message>> SearchMessagesAsync(Guid userId, string searchTerm);
        Task<Message> AddMessageAsync(Message message);
        Task<Message> UpdateMessageAsync(Message message);
        Task<List<Guid>> GetChatPartnersAsync(Guid userId);
    }
}
