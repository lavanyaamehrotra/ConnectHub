using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.DTOs
{
    // ========== REQUEST DTOS ==========

    public class CreateRoomRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string RoomType { get; set; } = "PUBLIC"; // PUBLIC, PRIVATE, DIRECT

        public string? AvatarUrl { get; set; }

        public int MaxMembers { get; set; } = 500;
    }

    public class UpdateRoomRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string RoomName { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? AvatarUrl { get; set; }

        public int? MaxMembers { get; set; }
    }

    public class SendRoomMessageRequest
    {
        [MaxLength(2000)]
        public string? Content { get; set; } = string.Empty;

        public string? MediaUrl { get; set; }
        public string? MessageType { get; set; } // TEXT, IMAGE, FILE
    }

    public class AddMemberRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Username { get; set; } = string.Empty;
    }

    public class MakeAdminRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }

    public class UpdateMemberRoleRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public string Role { get; set; } = string.Empty; // ADMIN, MODERATOR, MEMBER
    }

    // ========== RESPONSE DTOS ==========

    public class ChatRoomResponse
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string RoomType { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public Guid CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public int MaxMembers { get; set; }
        public int MemberCount { get; set; }
    }

    public class MemberResponse
    {
        public Guid UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
        public Guid? LastReadMessageId { get; set; }
        public DateTime? LastReadAt { get; set; }
    }

    public class RoomMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid RoomId { get; set; }
        public Guid SenderId { get; set; }
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public string? MessageType { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }
    }
}