using System.Threading.Channels;
using ConnectHub.NotificationService.Interfaces;
using ConnectHub.NotificationService.Messaging;

namespace ConnectHub.NotificationService.Services
{
    // ============================================================
    // InMemoryNotificationPublisher
    //
    // A more reliable alternative to RabbitMQ for Render free tier.
    // Uses System.Threading.Channels to pass events to a background
    // worker without needing an external message broker.
    // ============================================================
    public class InMemoryNotificationPublisher : INotificationPublisher
    {
        private readonly Channel<NotificationEvent> _channel;
        private readonly ILogger<InMemoryNotificationPublisher> _logger;

        public InMemoryNotificationPublisher(Channel<NotificationEvent> channel, ILogger<InMemoryNotificationPublisher> logger)
        {
            _channel = channel;
            _logger = logger;
        }

        public async Task PublishAsync(NotificationEvent evt)
        {
            try
            {
                await _channel.Writer.WriteAsync(evt);
                _logger.LogDebug("Published notification {Id} to in-memory channel", evt.NotificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write to in-memory channel for {Id}", evt.NotificationId);
            }
        }
    }
}
