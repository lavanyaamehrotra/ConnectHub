using ConnectHub.ChatRoomService.Models;

namespace ConnectHub.ChatRoomService.Interfaces
{
    public interface IChatRoomRepository
    {
        Task<ChatRoom?> FindByRoomIdAsync(Guid roomId);
        Task<List<ChatRoom>> FindByCreatedByAsync(Guid userId);
        Task<ChatRoom?> FindByRoomNameAsync(string roomName);
        Task<List<ChatRoom>> FindRoomsByUserIdAsync(Guid userId);
        Task<List<RoomMember>> FindMembersByRoomIdAsync(Guid roomId);
        Task<bool> IsUserInRoomAsync(Guid userId, Guid roomId);
        Task<int> CountMembersByRoomIdAsync(Guid roomId);
        Task<List<ChatRoom>> FindPublicRoomsAsync();
        
        Task AddRoomAsync(ChatRoom room);
        Task UpdateRoomAsync(ChatRoom room);
        Task AddMemberAsync(RoomMember member);
        Task UpdateMemberAsync(RoomMember member);
        Task AddMessageAsync(RoomMessage message);
        Task UpdateMessageAsync(RoomMessage message);
        Task<RoomMessage?> FindMessageByIdAsync(Guid messageId);
        Task<List<RoomMessage>> GetRoomMessagesAsync(Guid roomId, int skip, int take);
        Task MarkMessagesAsReadUntilAsync(Guid roomId, DateTime sentAt);
    }
}
