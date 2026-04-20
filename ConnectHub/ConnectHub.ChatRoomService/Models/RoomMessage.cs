using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.Models
{
    /// <summary>
    /// ROOM MESSAGE - Represents a message sent in a chat room
    /// </summary>
    public class RoomMessage
    {
        [Key]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid SenderId { get; set; }

        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ChatRoom Room { get; set; } = null!;
    }
}