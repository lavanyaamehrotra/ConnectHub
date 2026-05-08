using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.Models
{
    /// <summary>
    /// 👥 ROOM MEMBER - Represents a user's membership in a chat room
    /// </summary>
    public class RoomMember
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Role { get; set; } = "MEMBER"; // ADMIN, MODERATOR, MEMBER

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public bool IsActive { get; set; } = true;
        
        public Guid? LastReadMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }

        // Navigation property
        public ChatRoom Room { get; set; } = null!;
    }
}