using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using ConnectHub.HubService.Interfaces;
using ConnectHub.HubService.Presence;
using ConnectHub.HubService.Services;

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
        private readonly INotificationServiceClient _notificationService;
        private readonly IAuthServiceClient _authServiceClient;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(
            IMessageService  messageService,
            IChatRoomService roomService,
            IPresenceService presenceService,
            INotificationServiceClient notificationService,
            IAuthServiceClient authServiceClient,
            ILogger<ChatHub> logger)
        {
            _messageService  = messageService;
            _roomService     = roomService;
            _presenceService = presenceService;
            _notificationService = notificationService;
            _authServiceClient = authServiceClient;
            _logger          = logger;
        }

        // ===========================================================
        // LIFECYCLE — OnConnectedAsync
        // ===========================================================
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            var token = GetAccessToken();

            // Register connection in Redis
            await _presenceService.UserConnectedAsync(userId, Context.ConnectionId);

            // Sync with AuthService DB
            if (!string.IsNullOrEmpty(token))
            {
                await _authServiceClient.UpdatePresenceAsync(userId, true, token);
            }

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
            var token = GetAccessToken();

            await _presenceService.UserDisconnectedAsync(userId, Context.ConnectionId);

            // Only broadcast offline when user has NO connections left
            if (!await _presenceService.IsUserOnlineAsync(userId))
            {
                // Sync with AuthService DB
                if (!string.IsNullOrEmpty(token))
                {
                    await _authServiceClient.UpdatePresenceAsync(userId, false, token);
                }

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

            // Notify if offline
            if (!await _presenceService.IsUserOnlineAsync(receiverId))
            {
                _logger.LogInformation("Recipient {ReceiverId} is offline. Triggering notification...", receiverId);
                await _notificationService.SendNotificationAsync(
                    receiverId, 
                    senderId, 
                    "MESSAGE", 
                    "New Message", 
                    content, 
                    null, 
                    null, 
                    token);
            }
            else
            {
                _logger.LogInformation("Recipient {ReceiverId} is online. Skipping notification.", receiverId);
            }

            _logger.LogInformation("Direct message from {Sender} to {Receiver}", senderId, receiverId);
        }

        public async Task SendMediaMessage(Guid receiverId, string content, string mediaUrl, string messageType)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            var savedMessage = await _messageService.SendMediaMessageAsync(senderId, receiverId, content, mediaUrl, messageType, token);

            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", savedMessage);
            await Clients.Caller.SendAsync("MessageSent", savedMessage);

            // Notify if offline
            if (!await _presenceService.IsUserOnlineAsync(receiverId))
            {
                await _notificationService.SendNotificationAsync(
                    receiverId, 
                    senderId, 
                    messageType, 
                    $"New {messageType}", 
                    content ?? $"Sent a {messageType}", 
                    null, 
                    null, 
                    token);
            }

            _logger.LogInformation("Media message ({Type}) from {Sender} to {Receiver}", messageType, senderId, receiverId);
        }

        // ===========================================================
        // ROOM (GROUP) MESSAGING
        // ===========================================================
        public async Task JoinRoom(Guid roomId)
        {
            var userId = GetUserId();
            await Groups.AddToGroupAsync(Context.ConnectionId, roomId.ToString("D"));
            await Clients.OthersInGroup(roomId.ToString("D")).SendAsync("UserJoinedRoom", new
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
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId.ToString("D"));
            await Clients.OthersInGroup(roomId.ToString("D")).SendAsync("UserLeftRoom", new
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

        public async Task SendRoomMediaMessage(Guid roomId, string content, string mediaUrl, string messageType)
        {
            var senderId = GetUserId();
            var token    = GetAccessToken();

            var savedMessage = await _roomService.SendRoomMediaMessageAsync(senderId, roomId, content, mediaUrl, messageType, token);
            await Clients.Group(roomId.ToString()).SendAsync("ReceiveRoomMessage", savedMessage);

            _logger.LogInformation("Room media message ({Type}) from {Sender} in room {RoomId}", messageType, senderId, roomId);
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
        public async Task EditMessage(Guid messageId, string newContent)
        {
            var senderId = GetUserId();
            var token = GetAccessToken();

            var updatedMessage = await _messageService.EditMessageAsync(senderId, messageId, newContent, token);
            if (updatedMessage == null) return;
            
            // Notify both sender and receiver
            await Clients.User(senderId.ToString()).SendAsync("MessageEdited", updatedMessage);
            await Clients.User(updatedMessage.ReceiverId.ToString()).SendAsync("MessageEdited", updatedMessage);
        }

        public async Task DeleteMessage(Guid messageId)
        {
            var userId = GetUserId();
            var token = GetAccessToken();

            // We need to know who the receiver is to notify them
            var message = await _messageService.GetMessageByIdAsync(userId, messageId, token);
            if (message == null) return;

            var success = await _messageService.DeleteMessageAsync(userId, messageId, token);
            if (success)
            {
                await Clients.User(message.SenderId.ToString()).SendAsync("MessageDeleted", messageId);
                await Clients.User(message.ReceiverId.ToString()).SendAsync("MessageDeleted", messageId);
            }
        }

        public async Task EditRoomMessage(Guid messageId, string newContent)
        {
            var userId = GetUserId();
            var token  = GetAccessToken();

            var updatedMessage = await _roomService.UpdateRoomMessageAsync(userId, messageId, newContent, token);
            
            if (updatedMessage is RoomMessageDto msg && msg.RoomId != Guid.Empty)
            {
                await Clients.Group(msg.RoomId.ToString("D")).SendAsync("RoomMessageEdited", updatedMessage);
            }
            else
            {
                // Fallback: Broadcast to caller
                await Clients.Caller.SendAsync("RoomMessageEdited", updatedMessage);
            }
        }

        public async Task DeleteRoomMessage(Guid messageId)
        {
            var userId = GetUserId();
            var token  = GetAccessToken();

            var roomId = await _roomService.DeleteRoomMessageAsync(userId, messageId, token);
            if (roomId.HasValue)
            {
                await Clients.Group(roomId.Value.ToString("D")).SendAsync("RoomMessageDeleted", messageId);
            }
        }

        public async Task MarkMessageRead(Guid messageId, Guid senderId)
        {
            var readerId = GetUserId();
            var token    = GetAccessToken();

            // Broadcast to the sender immediately (optimistic UI)
            await Clients.User(senderId.ToString()).SendAsync("MessageRead", new
            {
                MessageId = messageId,
                ReadBy    = readerId,
                ReadAt    = DateTime.UtcNow
            });

            // Update the database in the background
            await _messageService.MarkAsReadAsync(messageId, token);
            _logger.LogInformation("Message {MsgId} read by {ReaderId}", messageId, readerId);
        }

        public async Task MarkRoomMessageRead(Guid roomId, Guid messageId)
        {
            var readerId = GetUserId();
            var token    = GetAccessToken();

            var fullyRead = await _roomService.MarkRoomMessageReadAsync(roomId, messageId, token);
            
            if (fullyRead)
            {
                // If it's now fully read, notify the entire group
                await Clients.Group(roomId.ToString()).SendAsync("RoomMessageRead", new
                {
                    MessageId = messageId,
                    RoomId    = roomId,
                    IsRead    = true,
                    ReadAt    = DateTime.UtcNow
                });
            }
            
            _logger.LogInformation("Room message {MsgId} marked read by {ReaderId} in room {RoomId}. FullyRead: {FullyRead}", messageId, readerId, roomId, fullyRead);
        }

        public async Task MarkAllAsRead(Guid otherUserId)
        {
            var readerId = GetUserId();
            var token    = GetAccessToken();

            // Notify the other user (the sender of the messages we just read)
            await Clients.User(otherUserId.ToString()).SendAsync("AllMessagesRead", new
            {
                ReadBy = readerId,
                ReadAt = DateTime.UtcNow
            });

            // Update the database in the background
            await _messageService.MarkAllAsReadAsync(readerId, otherUserId, token);
            _logger.LogInformation("All messages from {OtherId} read by {ReaderId}", otherUserId, readerId);
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

        public async Task RequestOnlineUsers()
        {
            var onlineUsers = await _presenceService.GetOnlineUserIdsAsync();
            await Clients.Caller.SendAsync("OnlineUsers", onlineUsers);
            _logger.LogInformation("User {UserId} requested the online users list", GetUserId());
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