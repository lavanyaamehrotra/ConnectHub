using System;

namespace ConnectHub.HubService.Models
{
    public class MessageHubResponse
    {
        public Guid MessageId { get; set; }
        public Guid SenderId { get; set; }
        public Guid ReceiverId { get; set; }
        public string Content { get; set; } = string.Empty;
        public string MessageType { get; set; } = "TEXT";
        public string? MediaUrl { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public DateTime SentAt { get; set; }
    }
}
