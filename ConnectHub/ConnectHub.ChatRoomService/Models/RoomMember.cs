using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.Models
{
    /// <summary>
    ///  ROOM MEMBER - Represents a user's membership in a chat room
    /// </summary>
    public class RoomMember
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid RoomId { get; set; }

        [Required]
        public Guid UserId { get; set; }  // UserId from Auth Service

        [Required]
        public string Role { get; set; } = "Member";  // "Admin" or "Member"

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public ChatRoom Room { get; set; } = null!;
    }
}