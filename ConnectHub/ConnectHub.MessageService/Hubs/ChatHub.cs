using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ConnectHub.MessageService.Hubs
{
    /// <summary>
    /// 💬 CHAT HUB - Real-time messaging using SignalR
    /// 
    /// WHAT IS SIGNALR?
    /// - Enables real-time communication (WebSockets)
    /// - Server can push messages to clients instantly
    /// - No need for client to constantly ask "any new messages?"
    /// 
    /// [Authorize] - Only authenticated users can connect
    /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(ILogger<ChatHub> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 📤 SEND MESSAGE - Broadcast message to receiver in real-time
        /// 
        /// Called by client: connection.invoke("SendMessage", receiverId, message)
        /// 
        /// FLOW:
        /// 1. Get sender's UserId from JWT
        /// 2. Send message to specific receiver (by their UserId)
        /// 3. Also send confirmation back to sender
        /// </summary>
        public async Task SendMessage(Guid receiverId, object message)
        {
            var senderId = GetUserId();
            
            // Send to the specific receiver (by their UserId)
            await Clients.User(receiverId.ToString()).SendAsync("ReceiveMessage", senderId, message);
            
            // Also send confirmation back to sender
            await Clients.Caller.SendAsync("MessageSent", message);
            
            _logger.LogInformation("Real-time message sent from {SenderId} to {ReceiverId}", senderId, receiverId);
        }

        /// <summary>
        /// 🔌 ON CONNECTED - Called when a client connects
        /// Use this for tracking online users
        /// </summary>
        public override async Task OnConnectedAsync()
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} connected to ChatHub", userId);
            await base.OnConnectedAsync();
        }

        /// <summary>
        /// 🔌 ON DISCONNECTED - Called when a client disconnects
        /// Use this to update online status
        /// </summary>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetUserId();
            _logger.LogInformation("User {UserId} disconnected from ChatHub", userId);
            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Get UserId from JWT token
        /// SignalR automatically validates token on connection
        /// </summary>
        private Guid GetUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim ?? Guid.Empty.ToString());
        }
    }
}