using ConnectHub.ChatRoomService.DTOs;

namespace ConnectHub.ChatRoomService.Interfaces
{
    public interface IChatRoomService
    {
        // Room management
        Task<ChatRoomResponse> CreateRoomAsync(Guid userId, CreateRoomRequest request);
        Task<ChatRoomResponse> UpdateRoomAsync(Guid userId, Guid roomId, UpdateRoomRequest request);
        Task<bool> DeleteRoomAsync(Guid userId, Guid roomId);
        Task<List<ChatRoomResponse>> GetUserRoomsAsync(Guid userId);
        Task<ChatRoomResponse> GetRoomAsync(Guid roomId);

        // Member management
        Task<bool> JoinRoomAsync(Guid userId, Guid roomId);
        Task<bool> LeaveRoomAsync(Guid userId, Guid roomId);
        Task<List<MemberResponse>> GetRoomMembersAsync(Guid roomId);
        Task<bool> AddMemberAsync(Guid adminUserId, Guid roomId, AddMemberRequest request);
        Task<bool> RemoveMemberAsync(Guid adminUserId, Guid roomId, Guid memberUserId);
        Task<bool> MakeAdminAsync(Guid adminUserId, Guid roomId, MakeAdminRequest request);

        // Messaging
        Task<RoomMessageResponse> SendMessageAsync(Guid userId, Guid roomId, SendRoomMessageRequest request);
        Task<List<RoomMessageResponse>> GetRoomMessagesAsync(Guid roomId, int page = 1, int pageSize = 50);
    }
}