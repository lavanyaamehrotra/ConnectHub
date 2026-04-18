using System;
using System.ComponentModel.DataAnnotations;

namespace ConnectHub.MessageService.Models
{
    /// <summary>
    /// 💬 MESSAGE MODEL - Represents a direct message between two users
    /// This maps to "Messages" table in PostgreSQL
    /// </summary>
    public class Message
    {
        /// <summary>
        /// 🔑 PRIMARY KEY - Unique identifier for each message
        /// </summary>
        [Key]
        public Guid MessageId { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 👤 SENDER ID - User who sent the message (from Auth-Service)
        /// </summary>
        [Required]
        public Guid SenderId { get; set; }

        /// <summary>
        /// 👤 RECEIVER ID - User who receives the message
        /// </summary>
        [Required]
        public Guid ReceiverId { get; set; }

        /// <summary>
        /// 📝 CONTENT - The actual message text
        /// </summary>
        [Required]
        [MaxLength(2000)]
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// 👁️ IS READ - Has the receiver seen this message?
        /// </summary>
        public bool IsRead { get; set; } = false;

        /// <summary>
        /// ✏️ IS EDITED - Has the message been edited?
        /// </summary>
        public bool IsEdited { get; set; } = false;

        /// <summary>
        /// 🗑️ IS DELETED - Soft delete flag
        /// </summary>
        public bool IsDeleted { get; set; } = false;

        /// <summary>
        /// ⏰ SENT AT - When the message was sent
        /// </summary>
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// ✏️ EDITED AT - When the message was last edited
        /// </summary>
        public DateTime? EditedAt { get; set; }
    }
}