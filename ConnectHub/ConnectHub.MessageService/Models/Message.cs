using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ConnectHub.MessageService.Models
{
    public class Message
    {
        [Key]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid SenderId { get; set; }

        public Guid? ReceiverId { get; set; }

        public Guid? RoomId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        [MaxLength(20)]
        public string MessageType { get; set; } = "TEXT"; // TEXT, IMAGE, FILE, AUDIO

        [MaxLength(500)]
        public string? MediaUrl { get; set; }

        public bool IsRead { get; set; } = false;

        public DateTime? ReadAt { get; set; }

        public bool IsEdited { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public DateTime? EditedAt { get; set; }

        public Guid? ReplyToMessageId { get; set; }

        [ForeignKey("ReplyToMessageId")]
        public Message? ReplyToMessage { get; set; }
    }
}