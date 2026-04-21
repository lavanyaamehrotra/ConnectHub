using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.Models
{
    /// <summary>
    /// 🏠 CHAT ROOM - Represents a group chat room
    /// </summary>
    public class ChatRoom
    {
        [Key]
        public Guid RoomId { get; set; } = Guid.NewGuid();

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string RoomType { get; set; } = "PUBLIC"; // PUBLIC, PRIVATE, DIRECT

        [MaxLength(500)]
        public string? AvatarUrl { get; set; }

        [Required]
        public Guid CreatedBy { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;

        public int MaxMembers { get; set; } = 500;

        // Navigation properties
        public ICollection<RoomMember> Members { get; set; } = new List<RoomMember>();
        public ICollection<RoomMessage> Messages { get; set; } = new List<RoomMessage>();
    }
}