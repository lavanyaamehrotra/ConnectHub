using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.Models
{
    /// <summary>
    /// CHAT ROOM - Represents a group chat room
    /// </summary>
    public class ChatRoom
    {
        [Key]
        public Guid RoomId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public Guid CreatedBy { get; set; }  // UserId from Auth Service

        public bool IsPrivate { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
        public ICollection<RoomMessage> Messages { get; set; } = new List<RoomMessage>();
    }
}