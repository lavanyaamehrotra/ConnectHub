using System.ComponentModel.DataAnnotations;

namespace ConnectHub.MessageService.DTOs
{
    /// <summary>
    /// 📤 SEND MESSAGE REQUEST
    /// What the client sends when they want to send a message
    /// This is the INPUT format for the API
    /// </summary>
    public class SendMessageRequest
    {
        /// <summary>
        /// Who is receiving this message? (UserId from Auth Service)
        /// [Required] - Must be provided, cannot be empty
        /// </summary>
        [Required]
        public Guid ReceiverId { get; set; }

        /// <summary>
        /// The actual message text
        /// MinLength(1) - Cannot be empty
        /// MaxLength(2000) - Prevents abuse
        /// </summary>
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// ✏️ EDIT MESSAGE REQUEST
    /// What client sends when editing a message
    /// Only the content can be changed
    /// </summary>
    public class EditMessageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// 📦 MESSAGE RESPONSE
    /// What the API returns to the client
    /// This is the OUTPUT format
    /// Notice: No sensitive data, just message info
    /// </summary>
    public class MessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
    }

    /// <summary>
    /// 💬 CONVERSATION RESPONSE
    /// Returns all messages between two users
    /// Includes the other user's ID and all messages
    /// </summary>
    public class ConversationResponse
    {
        /// <summary>
        /// The ID of the person you're talking to
        /// </summary>
        public Guid OtherUserId { get; set; }
        
        /// <summary>
        /// List of all messages in this conversation
        /// Sorted oldest to newest
        /// </summary>
        public List<MessageResponse> Messages { get; set; } = new();
    }

    /// <summary>
    /// 🔍 SEARCH RESPONSE
    /// Returns messages matching search term
    /// </summary>
    public class SearchMessagesResponse
    {
        public List<MessageResponse> Messages { get; set; } = new();
        public int TotalCount { get; set; }
    }
}