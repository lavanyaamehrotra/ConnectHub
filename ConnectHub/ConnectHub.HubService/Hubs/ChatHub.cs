using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Presence;

namespace ConnectHub.HubService.Hubs
{
    // ============================================================
    // ChatHub — the real-time communication core.
    //
    // FROM CLASS DIAGRAM (Figure 5):
    //   _messageService  → IMessageService  (HTTP → MessageService)
    //   _roomService     → IChatRoomService (HTTP → ChatRoomService)
    //   _presenceService → IPresenceService (Singleton, in-memory)
    //
    // Inherits Hub; registered via app.MapHub<ChatHub>("/hubs/chat").
    // All Hub methods return Task; Context.UserIdentifier maps to
    // UserId via IUserIdProvider (CustomUserIdProvider).
    //
    // CLIENT CONNECTION (JavaScript):
    //   const conn = new signalR.HubConnectionBuilder()
    //       .withUrl("http://localhost:5006/hubs/chat", {
    //           accessTokenFactory: () => localStorage.getItem("token")
    //       })
    //       .withAutomaticReconnect()
    //       .build();
    //   await conn.start();
    // ============================================================

    [Authorize]
    public class ChatHub : Hub
    {
        // FROM CLASS DIAGRAM — three service dependencies:
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

        /// <summary>
        /// FROM CLASS DIAGRAM:
        /// "Calls IPresenceService.UserConnected(userId, connectionId);
        ///  broadcasts online status to all clients;
        ///  re-joins user to their rooms via Groups.AddToGroupAsync()"
        ///
        /// Steps:
        ///  1. Register connection in PresenceService
        ///  2. Re-join all the user's SignalR groups (rooms)
        ///  3. Broadcast UserOnline to all OTHER connected clients
        ///  4. Send current online list to the newly connected client
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();

            // Step 1 — register connection
            _presenceService.UserConnected(userId, Context.ConnectionId);

            // Step 2 — re-join room groups so the user still receives room
            // broadcasts after a page refresh / reconnect
            await RejoinUserRoomsAsync();

            // Step 3 — tell all OTHER clients this user came online
            await Clients.Others.SendAsync("UserOnline", userId);

            // Step 4 — give the newly connected client the full online list
            var onlineUsers = _presenceService.GetOnlineUserIds();
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);

            _logger.LogInformation("User {UserId} connected. ConnId: {ConnId}",
                userId, Context.ConnectionId);

            await base.OnConnectedAsync();
        }

        // ===========================================================
        // LIFECYCLE — OnDisconnectedAsync
        // ===========================================================

        /// <summary>
        /// FROM CLASS DIAGRAM:
        /// "Calls IPresenceService.UserDisconnected(userId, connectionId);
        ///  broadcasts offline if no remaining connections for that user"
        ///
        /// Multi-device: only broadcast UserOffline when the LAST
        /// connection for a user closes.
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();

            // Remove this connection from the presence tracker
            _presenceService.UserDisconnected(userId, Context.ConnectionId);

            // Broadcast offline only when user has NO connections left
            if (!_presenceService.IsUserOnline(userId))
            {
                await Clients.Others.SendAsync("UserOffline", userId);
                _logger.LogInformation("User {UserId} is now offline", userId);
            }

            if (exception != null)
                _logger.LogWarning("User {UserId} disconnected with error: {Msg}",
                    userId, exception.Message);

            await base.OnDisconnectedAsync(exception);
        }

        // ===========================================================
        // DIRECT MESSAGING — SendDirectMessage
        // ===========================================================

        /// <summary>
        /// FROM CLASS DIAGRAM (Hub Method):
        /// "Persists Message via IMessageService.SendMessage();
        ///  calls Clients.User(receiverId).SendAsync('ReceiveMessage', message)"
        ///
        /// Flow:
        ///  1. Delegate persistence to IMessageService (→ MessageService REST)
        ///  2. Push saved message to receiver (all devices)
        ///  3. Echo back to sender
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("SendDirectMessage", receiverGuid, "Hello!");
        /// CLIENT LISTENS:
        ///   connection.on("ReceiveMessage", msg => display(msg));
        ///   connection.on("MessageSent",    msg => addToMyChat(msg));
        /// </summary>
        public async Task SendDirectMessage(Guid receiverId, string content)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            // Persist via IMessageService → MessageService REST API
            var savedMessage = await _messageService.SendMessageAsync(senderId, receiverId, content, token);

            // Push to all of the receiver's devices
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", savedMessage);

            // Echo back to sender
            await Clients.Caller.SendAsync("MessageSent", savedMessage);

            _logger.LogInformation("Direct message from {Sender} to {Receiver}", senderId, receiverId);
        }

        // ===========================================================
        // ROOM (GROUP) MESSAGING
        // ===========================================================

        /// <summary>
        /// JoinRoom — add this connection to the room's SignalR Group.
        /// Groups.AddToGroupAsync → connection receives all Clients.Group() broadcasts.
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("JoinRoom", roomGuid);
        /// </summary>
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

        /// <summary>
        /// LeaveRoom — remove this connection from the room's SignalR Group.
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("LeaveRoom", roomGuid);
        /// </summary>
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

        /// <summary>
        /// FROM CLASS DIAGRAM (Hub Method):
        /// "Persists Message with RoomId; calls
        ///  Clients.Group(roomId.ToString()).SendAsync('ReceiveRoomMessage', message)"
        ///
        /// Flow:
        ///  1. Delegate persistence to IChatRoomService (→ ChatRoomService REST)
        ///  2. Broadcast saved message to ALL members of the room group
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("SendRoomMessage", roomGuid, "Hello room!");
        /// CLIENT LISTENS:
        ///   connection.on("ReceiveRoomMessage", msg => displayRoomMsg(msg));
        /// </summary>
        public async Task SendRoomMessage(Guid roomId, string content)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            // Persist via IChatRoomService → ChatRoomService REST API
            var savedMessage = await _roomService.SendRoomMessageAsync(senderId, roomId, content, token);

            // Broadcast to ALL connections in the room group
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", savedMessage);

            _logger.LogInformation("Room message from {Sender} in room {RoomId}", senderId, roomId);
        }

        // ===========================================================
        // TYPING INDICATOR
        // ===========================================================

        /// <summary>
        /// FROM CLASS DIAGRAM (Hub Method):
        /// "Calls Clients.User(recipientId).SendAsync('UserTyping', senderId, isTyping)
        ///  — no DB persistence"
        ///
        /// CLIENT INVOKES (start/stop):
        ///   await connection.invoke("TypingIndicator", recipientGuid, true);
        /// CLIENT LISTENS:
        ///   connection.on("UserTyping", data => showOrHideDots(data));
        /// </summary>
        public async Task TypingIndicator(Guid recipientId, bool isTyping)
        {
            var senderId = GetUserId();

            await Clients.User(recipientId.ToString()).SendAsync("UserTyping", new
            {
                SenderId = senderId,
                IsTyping = isTyping
            });
        }

        /// <summary>
        /// RoomTypingIndicator — "John is typing..." for group chats.
        /// Sends to everyone in the room EXCEPT the person typing.
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("RoomTypingIndicator", roomGuid, true);
        /// </summary>
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

        /// <summary>
        /// MarkMessageRead — update read status and notify the original sender.
        ///
        /// Flow:
        ///  1. Delegate IsRead=true + ReadAt=now to IMessageService
        ///  2. Push "MessageRead" to the original sender → their UI shows "Seen"
        ///
        /// CLIENT INVOKES (when receiver opens the message):
        ///   await connection.invoke("MarkMessageRead", messageGuid, senderGuid);
        /// CLIENT LISTENS (original sender):
        ///   connection.on("MessageRead", data => showSeenTick(data.messageId));
        /// </summary>
        public async Task MarkMessageRead(Guid messageId, Guid senderId)
        {
            var readerId = GetUserId();
            var token    = GetAccessToken();

            // Update read status via IMessageService
            await _messageService.MarkAsReadAsync(messageId, token);

            // Notify the original sender
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
        // (REST handles DB update; Hub pushes real-time UI sync)
        // ===========================================================

        /// <summary>
        /// NotifyMessageEdited — push edit event to receiver's UI.
        /// Client calls PUT /api/messages/edit/{id} on MessageService first,
        /// then invokes this to update the receiver's screen in real-time.
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("NotifyMessageEdited", msgGuid, receiverGuid, "new text");
        /// </summary>
        public async Task NotifyMessageEdited(Guid messageId, Guid receiverId, string newContent)
        {
            await Clients.User(receiverId.ToString()).SendAsync("MessageEdited", new
            {
                MessageId  = messageId,
                NewContent = newContent,
                EditedAt   = DateTime.UtcNow
            });
        }

        /// <summary>
        /// NotifyMessageDeleted — push delete event to receiver's UI.
        /// Client calls DELETE /api/messages/delete/{id} on MessageService first,
        /// then invokes this to remove the message from the receiver's screen.
        ///
        /// CLIENT INVOKES:
        ///   await connection.invoke("NotifyMessageDeleted", msgGuid, receiverGuid);
        /// </summary>
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

        /// <summary>
        /// Re-join all SignalR groups for rooms this user belongs to.
        /// Called from OnConnectedAsync so the user receives room
        /// broadcasts even after a page refresh / reconnect.
        /// </summary>
        private async Task RejoinUserRoomsAsync()
        {
            var token   = GetAccessToken();
            var roomIds = await _roomService.GetUserRoomIdsAsync(token);

            var userId = GetUserId();
            foreach (var roomId in roomIds)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString());
                _logger.LogInformation("User {UserId} re-joined room {RoomId}", userId, roomId);
            }
        }

        /// <summary>
        /// Extract the current user's Guid from the JWT.
        /// Context.UserIdentifier is set by CustomUserIdProvider.
        /// </summary>
        private Guid GetUserId()
        {
            var userIdStr = Context.UserIdentifier;
            return Guid.TryParse(userIdStr, out var userId) ? userId : Guid.Empty;
        }

        /// <summary>
        /// Read the JWT from the SignalR query string so it can be
        /// forwarded to downstream REST services.
        /// </summary>
        private string? GetAccessToken()
            => Context.GetHttpContext()?.Request.Query["access_token"].ToString();
    }
}