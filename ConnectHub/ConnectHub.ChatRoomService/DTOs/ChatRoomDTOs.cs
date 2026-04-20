using System.ComponentModel.DataAnnotations;

namespace ConnectHub.ChatRoomService.DTOs
{
    // ========== REQUEST DTOS ==========

    /// <summary>
    ///  CREATE ROOM REQUEST
    /// </summary>
    public class CreateRoomRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public bool IsPrivate { get; set; } = false;
    }

    /// <summary>
    ///  UPDATE ROOM REQUEST
    /// </summary>
    public class UpdateRoomRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// SEND MESSAGE REQUEST
    /// </summary>
    public class SendRoomMessageRequest
    {
        [Required]
        [MinLength(1)]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
   ///  ADD MEMBER REQUEST
    /// </summary>
    public class AddMemberRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }

    /// <summary>
    ///  MAKE ADMIN REQUEST
    /// </summary>
    public class MakeAdminRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }

    // ========== RESPONSE DTOS ==========

    /// <summary>
    ///  CHAT ROOM RESPONSE
    /// </summary>
    public class ChatRoomResponse
    {
        public Guid RoomId { get; set; }
        public string Name { get; set; } = string.Empty;
        public Guid CreatedBy { get; set; }
        public bool IsPrivate { get; set; }
        public DateTime CreatedAt { get; set; }
        public int MemberCount { get; set; }
    }

    /// <summary>
    /// MEMBER RESPONSE
    /// </summary>
    public class MemberResponse
    {
        public Guid UserId { get; set; }
        public string Role { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
    }

    /// <summary>
    /// MESSAGE RESPONSE
    /// </summary>
    public class RoomMessageResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool IsDeleted { get; set; }
    }
}