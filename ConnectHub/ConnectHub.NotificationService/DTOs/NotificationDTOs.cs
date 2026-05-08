namespace ConnectHub.NotificationService.DTOs
{
    // DTO returned to clients
    public class NotificationDto
    {
        public Guid    NotificationId { get; set; }
        public Guid    RecipientId    { get; set; }
        public Guid?   SenderId       { get; set; }
        public string  Type           { get; set; } = "";
        public string  Title          { get; set; } = "";
        public string  Message        { get; set; } = "";
        public Guid?   RelatedId      { get; set; }
        public string? RelatedType    { get; set; }
        public bool    IsRead         { get; set; }
        public DateTime SentAt        { get; set; }
    }

    // Used by other microservices to create a notification
    public class CreateNotificationDto
    {
        public Guid    RecipientId  { get; set; }
        public Guid?   SenderId     { get; set; }
        public string  Type         { get; set; } = "MESSAGE";
        public string  Title        { get; set; } = "";
        public string  Message      { get; set; } = "";
        public Guid?   RelatedId    { get; set; }
        public string? RelatedType  { get; set; }
    }

    // Used by admin bulk-send endpoint
    public class BulkNotificationDto
    {
        public List<Guid> RecipientIds { get; set; } = new();
        public string Type             { get; set; } = "PLATFORM";
        public string Title            { get; set; } = "";
        public string Message          { get; set; } = "";
    }
}
