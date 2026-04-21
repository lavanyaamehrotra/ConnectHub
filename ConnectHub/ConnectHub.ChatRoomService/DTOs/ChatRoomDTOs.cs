using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.DTOs
{
    // ========== REQUEST DTOS ==========

    public class CreateRoomRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

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
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? AvatarUrl { get; set; }

        public int? MaxMembers { get; set; }
    }

    public class SendRoomMessageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    public class AddMemberRequest
    {
        [Required]
        public Guid UserId { get; set; }
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
        public string Name { get; set; } = string.Empty;
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
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoomMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}