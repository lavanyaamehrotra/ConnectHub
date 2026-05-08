using System.ComponentModel.DataAnnotations;

namespace ConnectHub.MessageService.DTOs
{
    // ========== REQUEST DTOS ==========

    public class SendMessageRequest
    {
        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public class SendMediaMessageRequest
    {
        [Required]
        public Guid ReceiverId { get; set; }

        [Required]
        public string MessageType { get; set; } = "TEXT"; // TEXT, IMAGE, FILE, AUDIO

        public string? MediaUrl { get; set; }

        public string? Content { get; set; }

        public Guid? ReplyToMessageId { get; set; }
    }

    public class EditMessageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    // ========== RESPONSE DTOS ==========

    public class MessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "TEXT";
        public string? MediaUrl { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public Guid? ReplyToMessageId { get; set; }
    }

    public class ConversationResponse
    {
        public Guid OtherUserId { get; set; }
        public List<MessageResponse> Messages { get; set; } = new();
    }

    public class SearchMessagesResponse
    {
        public List<MessageResponse> Messages { get; set; } = new();
        public int TotalCount { get; set; }
    }

    public class RecentChatResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public MessageResponse LastMessage { get; set; } = null!;
        public int UnreadCount { get; set; }
        public bool IsOnline { get; set; }
        public DateTime LastSeen { get; set; }
    }

    public class UnreadCountResponse
    {
        public int TotalUnread { get; set; }
        public Dictionary<Guid, int> UnreadByUser { get; set; } = new();
    }
}