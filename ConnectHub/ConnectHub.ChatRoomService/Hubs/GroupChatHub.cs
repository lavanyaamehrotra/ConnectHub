using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConnectHub.ChatRoomService.Hubs
{
    [Authorize]
    public class GroupChatHub : Hub
    {
        private readonly ILogger<GroupChatHub> _logger;

        public GroupChatHub(ILogger<GroupChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Join a chat room group (for receiving messages)
        /// </summary>
        public async Task JoinRoom(string roomId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            _logger.LogInformation("User {UserId} joined room {RoomId}", GetUserId(), roomId);
        }

        /// <summary>
        /// Leave a chat room group
        /// </summary>
        public async Task LeaveRoom(string roomId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            _logger.LogInformation("User {UserId} left room {RoomId}", GetUserId(), roomId);
        }

        /// <summary>
        /// Send message to a specific room
        /// </summary>
        public async Task SendMessageToRoom(string roomId, object message)
        {
            var userId = GetUserId();
            await Clients.Group(roomId).SendAsync("ReceiveMessage", userId, message);
            _logger.LogInformation("Message sent to room {RoomId} by {UserId}", roomId, userId);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to GroupChatHub", userId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from GroupChatHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}