using System.ComponentModel.DataAnnotations;

namespace ConnectHub.NotificationService.Models
{
    // ============================================================
    // UC5 — Notification entity
    // Matches class diagram: NotificationId, RecipientId, SenderId,
    // InvitationId, Type, Title, Message, RelatedType, IsRead, SentAt
    // ============================================================
    public class Notification
    {
        public Guid   NotificationId { get; set; } = Guid.NewGuid();
        public Guid   RecipientId    { get; set; }
        public Guid?  SenderId       { get; set; }   // null for system notifications
        public Guid?  RelatedId      { get; set; }   // message/room/invite id

        [Required]
        public string Type           { get; set; } = "MESSAGE"; // MESSAGE, MENTION, ROLE_CHANGE, INVITE, PLATFORM

        [Required]
        public string Title          { get; set; } = "";

        [Required]
        public string Message        { get; set; } = "";

        public string? RelatedType   { get; set; }   // "message", "room", "invite"

        public bool    IsRead        { get; set; } = false;
        public DateTime SentAt       { get; set; } = DateTime.UtcNow;
    }
}
