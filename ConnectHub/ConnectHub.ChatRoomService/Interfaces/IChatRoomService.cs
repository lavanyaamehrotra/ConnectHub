using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ConnectHub.ChatRoomService.DTOs;

namespace ConnectHub.ChatRoomService.Interfaces
{
    public interface IChatRoomService
    {
        // Room management
        Task<ChatRoomResponse> CreateRoom(Guid userId, string username, CreateRoomRequest request);
        Task<ChatRoomResponse> UpdateRoom(Guid userId, Guid roomId, UpdateRoomRequest request);
        Task<bool> DeleteRoom(Guid userId, Guid roomId);
        Task<List<ChatRoomResponse>> GetRoomsByUser(Guid userId);
        Task<ChatRoomResponse> GetRoomById(Guid roomId);
        Task<List<ChatRoomResponse>> GetPublicRooms();
        Task<bool> IsUserInRoom(Guid userId, Guid roomId);
        Task<int> GetMemberCount(Guid roomId);

        // Member management
        Task<bool> JoinRoom(Guid userId, string username, Guid roomId);
        Task<bool> LeaveRoom(Guid userId, Guid roomId);
        Task<List<MemberResponse>> GetMembers(Guid roomId);
        Task<bool> AddMember(Guid adminUserId, Guid roomId, AddMemberRequest request);
        Task<bool> RemoveMember(Guid adminUserId, Guid roomId, Guid memberUserId);
        Task<bool> UpdateMemberRole(Guid adminUserId, Guid roomId, UpdateMemberRoleRequest request);

        // Messaging (Internal use or helper)
        Task<RoomMessageResponse> SendMessage(Guid userId, Guid roomId, SendRoomMessageRequest request);
        Task<List<RoomMessageResponse>> GetRoomMessages(Guid roomId, int page = 1, int pageSize = 50);
        Task<bool> MarkRoomMessageAsRead(Guid userId, Guid roomId, Guid messageId);
        Task<RoomMessageResponse> UpdateMessage(Guid userId, Guid messageId, string newContent);
        Task<Guid?> DeleteMessage(Guid userId, Guid messageId);
    }
}