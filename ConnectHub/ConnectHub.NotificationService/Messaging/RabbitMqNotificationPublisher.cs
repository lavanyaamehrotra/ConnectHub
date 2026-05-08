using System.Text;
using System.Text.Json;
using RabbitMQ.Client;

namespace ConnectHub.NotificationService.Messaging
{
    // ============================================================
    // RabbitMQ NotificationPublisher
    //
    // WHY RABBITMQ?
    //   • NotificationService receives thousands of events (messages,
    //     mentions, room invites). Sending real-time badge pushes and
    //     emails synchronously inside SendAsync blocks the HTTP thread
    //     and creates tight coupling between the notification pipeline
    //     and the HubService / SMTP server.
    //   • RabbitMQ decouples the write path: NotificationService saves
    //     to DB, publishes an event, and returns immediately.
    //     The NotificationConsumer background service picks it up and
    //     handles the badge push + email asynchronously.
    //
    // TOPOLOGY:
    //   Exchange : "notifications" (type: direct, durable)
    //   Queue    : "notification.dispatch" (durable, dead-letter → "notification.dlq")
    //   Routing  : key "dispatch"
    //
    // DURABILITY:
    //   - Exchange and queue are durable → survive broker restart.
    //   - Messages are published with persistent delivery mode.
    //   - Dead-letter queue catches failed messages after max retries.
    // ============================================================
    public class RabbitMqNotificationPublisher : INotificationPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel      _channel;
        private readonly ILogger<RabbitMqNotificationPublisher> _logger;

        private const string ExchangeName = "notifications";
        private const string QueueName    = "notification.dispatch";
        private const string DlqName      = "notification.dlq";
        private const string RoutingKey   = "dispatch";

        public RabbitMqNotificationPublisher(
            IConfiguration config,
            ILogger<RabbitMqNotificationPublisher> logger)
        {
            _logger = logger;

            var factory = new ConnectionFactory();
            
            // Priority 1: Use full AMQP URL if provided (best for CloudAMQP)
            if (!string.IsNullOrEmpty(config["RabbitMQ:Url"]))
            {
                factory.Uri = new Uri(config["RabbitMQ:Url"]);
            }
            else 
            {
                // Priority 2: Use individual components (fallback)
                factory.HostName    = config["RabbitMQ:Host"]     ?? "localhost";
                factory.Port        = int.Parse(config["RabbitMQ:Port"] ?? "5672");
                factory.UserName    = config["RabbitMQ:Username"] ?? "guest";
                factory.Password    = config["RabbitMQ:Password"] ?? "guest";
                factory.VirtualHost = config["RabbitMQ:VHost"]    ?? "/";
            }

            factory.DispatchConsumersAsync = true;

            _connection = factory.CreateConnection("NotificationService-Publisher");
            _channel    = _connection.CreateModel();

            // Declare dead-letter queue first (so main queue can reference it)
            _channel.QueueDeclare(
                queue:      DlqName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null);

            // Declare durable exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type:     ExchangeType.Direct,
                durable:  true);

            // Declare main queue with dead-letter routing
            _channel.QueueDeclare(
                queue:      QueueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"]    = "",       // default exchange
                    ["x-dead-letter-routing-key"] = DlqName,
                    ["x-message-ttl"]             = 86_400_000 // 24h TTL
                });

            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);

            // Publisher confirms — ensures the broker acknowledged the publish
            _channel.ConfirmSelect();

            _logger.LogInformation("RabbitMQ publisher connected. Exchange: {Ex}, Queue: {Q}",
                ExchangeName, QueueName);
        }

        public Task PublishAsync(NotificationEvent evt)
        {
            try
            {
                var body    = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
                var props   = _channel.CreateBasicProperties();
                props.Persistent    = true;    // survive broker restart
                props.ContentType   = "application/json";
                props.MessageId     = evt.NotificationId.ToString();
                props.Timestamp     = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

                _channel.BasicPublish(
                    exchange:   ExchangeName,
                    routingKey: RoutingKey,
                    basicProperties: props,
                    body:       body);

                // Wait for broker ack (synchronous confirm; acceptable for low-latency scenarios)
                _channel.WaitForConfirmsOrDie(TimeSpan.FromSeconds(5));

                _logger.LogDebug("Published notification event {Id} for user {UserId}",
                    evt.NotificationId, evt.RecipientId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish notification event {Id}", evt.NotificationId);
                // Don't rethrow — notification was already saved to DB.
                // The consumer will pick it up on retry or admin can resend.
            }

            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}