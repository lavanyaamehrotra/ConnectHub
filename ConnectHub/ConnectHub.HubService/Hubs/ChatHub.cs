using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Presence;

namespace ConnectHub.HubService.Hubs
{
    // ============================================================
    // UC4 Redis Update — ChatHub
    //
    // Only change vs original: all _presenceService calls are
    // now awaited because PresenceService methods are async (Redis I/O).
    // All business logic is identical.
    // ============================================================

    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService  _messageService;
        private readonly IChatRoomService _roomService;
        private readonly IPresenceService _presenceService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IMessageService  messageService,
            IChatRoomService roomService,
            IPresenceService presenceService,
            ILogger<ChatHub> logger)
        {
            _messageService  = messageService;
            _roomService     = roomService;
            _presenceService = presenceService;
            _logger          = logger;
        }

        // ===========================================================
        // LIFECYCLE — OnConnectedAsync
        // ===========================================================
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            // Register connection in Redis
            await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);

            // Re-join all the user's room groups
            await RejoinUserRoomsAsync();

            // Tell all OTHER clients this user came online
            await Clients.Others.SendAsync("UserOnline", userId);

            // Send the current online user list to the newly connected client
            var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);

            _logger.LogInformation("User {UserId} connected. ConnId: {ConnId}", userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        // ===========================================================
        // LIFECYCLE — OnDisconnectedAsync
        // ===========================================================
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);

            // Only broadcast offline when user has NO connections left
            if (!await _presenceService.IsUserOnlineAsync(userId))
            {
                await Clients.Others.SendAsync("UserOffline", userId);
                _logger.LogInformation("User {UserId} is now offline", userId);
            }

            if (exception != null)
                _logger.LogWarning("User {UserId} disconnected with error: {Msg}", userId, exception.Message);

            await base.OnDisconnectedAsync(exception);
        }

        // ===========================================================
        // DIRECT MESSAGING
        // ===========================================================
        public async Task SendDirectMessage(Guid receiverId, string content)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            var savedMessage = await _messageService.SendMessageAsync(senderId, receiverId, content, token);

            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", savedMessage);
            await Clients.Caller.SendAsync("MessageSent", savedMessage);

            _logger.LogInformation("Direct message from {Sender} to {Receiver}", senderId, receiverId);
        }

        // ===========================================================
        // ROOM (GROUP) MESSAGING
        // ===========================================================
        public async Task JoinRoom(Guid roomId)
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserJoinedRoom", new
            {
                UserId   = userId,
                RoomId   = roomId,
                JoinedAt = DateTime.UtcNow
            });
            _logger.LogInformation("User {UserId} joined room {RoomId}", userId, roomId);
        }

        public async Task LeaveRoom(Guid roomId)
        {
            var userId = GetUserId();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString());
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("UserLeftRoom", new
            {
                UserId = userId,
                RoomId = roomId,
                LeftAt = DateTime.UtcNow
            });
            _logger.LogInformation("User {UserId} left room {RoomId}", userId, roomId);
        }

        public async Task SendRoomMessage(Guid roomId, string content)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            var savedMessage = await _roomService.SendRoomMessageAsync(senderId, roomId, content, token);
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", savedMessage);

            _logger.LogInformation("Room message from {Sender} in room {RoomId}", senderId, roomId);
        }

        // ===========================================================
        // TYPING INDICATOR
        // ===========================================================
        public async Task TypingIndicator(Guid recipientId, bool isTyping)
        {
            var senderId = GetUserId();
            await Clients.User(recipientId.ToString()).SendAsync("UserTyping", new
            {
                SenderId = senderId,
                IsTyping = isTyping
            });
        }

        public async Task RoomTypingIndicator(Guid roomId, bool isTyping)
        {
            var senderId = GetUserId();
            await Clients.OthersInGroup(roomId.ToString()).SendAsync("RoomUserTyping", new
            {
                SenderId = senderId,
                RoomId   = roomId,
                IsTyping = isTyping
            });
        }

        // ===========================================================
        // READ RECEIPTS
        // ===========================================================
        public async Task MarkMessageRead(Guid messageId, Guid senderId)
        {
            var readerId = GetUserId();
            var token    = GetAccessToken();

            await _messageService.MarkAsReadAsync(messageId, token);
            await Clients.User(senderId.ToString()).SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReadBy    = readerId,
                ReadAt    = DateTime.UtcNow
            });

            _logger.LogInformation("Message {MsgId} read by {ReaderId}", messageId, readerId);
        }

        // ===========================================================
        // EDIT & DELETE NOTIFICATIONS
        // ===========================================================
        public async Task NotifyMessageEdited(Guid messageId, Guid receiverId, string newContent)
        {
            await Clients.User(receiverId.ToString()).SendAsync("MessageEdited", new
            {
                MessageId  = messageId,
                NewContent = newContent,
                EditedAt   = DateTime.UtcNow
            });
        }

        public async Task NotifyMessageDeleted(Guid messageId, Guid receiverId)
        {
            await Clients.User(receiverId.ToString()).SendAsync("MessageDeleted", new
            {
                MessageId = messageId,
                DeletedAt = DateTime.UtcNow
            });
        }

        // ===========================================================
        // PRIVATE HELPERS
        // ===========================================================
        private async Task RejoinUserRoomsAsync()
        {
            var token   = GetAccessToken();
            var roomIds = await _roomService.GetUserRoomIdsAsync(token);
            var userId  = GetUserId();
            foreach (var roomId in roomIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
                _logger.LogInformation("User {UserId} re-joined room {RoomId}", userId, roomId);
            }
        }

        private Guid GetUserId()
        {
            var userIdStr = Context.UserIdentifier;
            return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
        }

        private string? GetAccessToken()
            => Context.GetHttpContext()?.Request.Query["access_token"].ToString();
    }
}