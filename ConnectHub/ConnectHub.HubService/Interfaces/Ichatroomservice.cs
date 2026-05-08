using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ConnectHub.HubService.Interfaces
{
    // ============================================================
    // FROM CLASS DIAGRAM (ChatHub dependencies):
    // _roomService → IChatRoomService
    //
    // HubService calls ChatRoomService REST API to persist room
    // messages. This interface abstracts that HTTP call.
    // ============================================================
    public interface IChatRoomService
    {
        /// <summary>
        /// Persist a room message via ChatRoomService REST API.
        /// Returns the saved message payload.
        /// </summary>
        Task<object> SendRoomMessageAsync(Guid senderId, Guid roomId, string content, string? token = null);

        Task<object> SendRoomMediaMessageAsync(Guid senderId, Guid roomId, string content, string mediaUrl, string messageType, string? token = null);

        /// <summary>
        /// Fetch all rooms the given user is a member of.
        /// Used in OnConnectedAsync to re-join SignalR groups.
        /// </summary>
        Task<List<Guid>> GetUserRoomIdsAsync(string? token = null);

        Task<object> UpdateRoomMessageAsync(Guid userId, Guid messageId, string newContent, string? token = null);
        Task<Guid?> DeleteRoomMessageAsync(Guid userId, Guid messageId, string? token = null);
        Task<bool> MarkRoomMessageReadAsync(Guid roomId, Guid messageId, string? token = null);
    }
}