namespace ConnectHub.NotificationService.Messaging
{
    // ============================================================
    // INotificationPublisher
    //
    // Abstraction for publishing notification events to RabbitMQ.
    // This decouples the service from the broker implementation
    // and makes it easy to mock in tests.
    // ============================================================
    public interface INotificationPublisher
    {
        /// <summary>
        /// Publish a notification event to the RabbitMQ exchange.
        /// Fire-and-forget from the caller's perspective; the consumer
        /// handles retries and dead-lettering.
        /// </summary>
        Task PublishAsync(NotificationEvent evt);
    }

    // ============================================================
    // NotificationEvent — the message payload sent over RabbitMQ
    // ============================================================
    public class NotificationEvent
    {
        public Guid    NotificationId { get; set; }
        public Guid    RecipientId    { get; set; }
        public Guid?   SenderId       { get; set; }
        public string  Type           { get; set; } = "";
        public string  Title          { get; set; } = "";
        public string  Message        { get; set; } = "";
        public Guid?   RelatedId      { get; set; }
        public string? RelatedType    { get; set; }
        public int     UnreadCount    { get; set; }  // pre-computed so consumer doesn't query DB
        public DateTime SentAt        { get; set; }
    }
}