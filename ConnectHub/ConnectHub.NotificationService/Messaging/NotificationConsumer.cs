using System.Text;
using System.Text.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using MailKit.Net.Smtp;
using MimeKit;

namespace ConnectHub.NotificationService.Messaging
{
    // ============================================================
    // NotificationConsumer — BackgroundService
    //
    // FIX: ConnectAndConsume() now declares the full topology
    //   (DLQ → exchange → main queue → bind) before calling
    //   BasicConsume. Previously the consumer assumed the publisher
    //   singleton had already created the queue, but because
    //   AddSingleton is lazy the publisher may not be resolved yet
    //   at startup → RabbitMQ returned:
    //     "NOT_FOUND - no queue 'notification.dispatch' in vhost '/'"
    //   Declaring topology in both publisher AND consumer is safe:
    //   RabbitMQ's QueueDeclare / ExchangeDeclare are idempotent
    //   when arguments are identical.
    //
    // Uses RabbitMQ.Client v6.x API:
    //   - ConnectionFactory with DispatchConsumersAsync = true
    //   - AsyncEventingBasicConsumer for non-blocking message handling
    //   - Full BasicConsume overload — avoids ambiguous overload in v6.8.x
    //   - Manual ack (autoAck: false) for at-least-once delivery
    // ============================================================
    public class NotificationConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory          _scopeFactory;
        private readonly IConfiguration                _config;
        private readonly ILogger<NotificationConsumer> _logger;

        private IConnection? _connection;
        private IModel?      _channel;

        // Must match constants in RabbitMqNotificationPublisher exactly
        private const string ExchangeName = "notifications";
        private const string QueueName    = "notification.dispatch";
        private const string DlqName      = "notification.dlq";
        private const string RoutingKey   = "dispatch";
        private const int    MaxRetries   = 3;

        public NotificationConsumer(
            IServiceScopeFactory          scopeFactory,
            IConfiguration                config,
            ILogger<NotificationConsumer> logger)
        {
            _scopeFactory = scopeFactory;
            _config       = config;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give RabbitMQ a moment to be fully ready
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    ConnectAndConsume();
                    await Task.Delay(Timeout.Infinite, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NotificationConsumer connection lost. Reconnecting in 10s...");
                    CleanupConnection();
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }

            CleanupConnection();
        }

        private void ConnectAndConsume()
        {
            // DispatchConsumersAsync = true is REQUIRED for AsyncEventingBasicConsumer
            var factory = new ConnectionFactory
            {
                HostName               = _config["RabbitMQ:Host"]     ?? "localhost",
                Port                   = int.Parse(_config["RabbitMQ:Port"] ?? "5672"),
                UserName               = _config["RabbitMQ:Username"] ?? "guest",
                Password               = _config["RabbitMQ:Password"] ?? "guest",
                VirtualHost            = _config["RabbitMQ:VHost"]    ?? "/",
                DispatchConsumersAsync = true
            };

            _connection = factory.CreateConnection("NotificationService-Consumer");
            _channel    = _connection.CreateModel();

            // ── FIX: Declare full topology here ──────────────────────────
            // The consumer must declare the same topology as the publisher.
            // QueueDeclare / ExchangeDeclare are idempotent — safe to call
            // from both sides. This prevents NOT_FOUND if the consumer
            // starts before the publisher singleton is first resolved.

            // 1. Dead-letter queue (must exist before main queue references it)
            _channel.QueueDeclare(
                queue:      DlqName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null);

            // 2. Durable direct exchange
            _channel.ExchangeDeclare(
                exchange: ExchangeName,
                type:     ExchangeType.Direct,
                durable:  true);

            // 3. Main queue with DLQ and TTL (identical args to publisher)
            _channel.QueueDeclare(
                queue:      QueueName,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments: new Dictionary<string, object>
                {
                    ["x-dead-letter-exchange"]    = "",          // default exchange
                    ["x-dead-letter-routing-key"] = DlqName,
                    ["x-message-ttl"]             = 86_400_000   // 24 h TTL
                });

            // 4. Bind queue to exchange
            _channel.QueueBind(QueueName, ExchangeName, RoutingKey);
            // ─────────────────────────────────────────────────────────────

            // Process one message at a time — prevents memory overflow
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceivedAsync;

            // Full overload — avoids ambiguous match compile error in v6.8.x
            _channel.BasicConsume(
                queue:       QueueName,
                autoAck:     false,
                consumerTag: "",
                noLocal:     false,
                exclusive:   false,
                arguments:   null,
                consumer:    consumer);

            _logger.LogInformation("NotificationConsumer listening on '{Q}'", QueueName);
        }

        private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            NotificationEvent? evt = null;
            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                evt = JsonSerializer.Deserialize<NotificationEvent>(json);

                if (evt == null)
                {
                    _logger.LogWarning("Received null event — nacking to DLQ");
                    _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
                    return;
                }

                _logger.LogDebug("Processing notification {Id} for user {UserId}",
                    evt.NotificationId, evt.RecipientId);

                // 1. Push real-time badge to HubService → SignalR → browser
                await PushBadgeAsync(evt.RecipientId, evt.UnreadCount);

                // 2. Send email via MailKit (only if SMTP is configured)
                await TrySendEmailAsync(evt.RecipientId, evt.Title, evt.Message);

                _channel!.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing notification {Id}", evt?.NotificationId);

                var deliveryCount = GetDeliveryCount(ea);
                bool requeue = deliveryCount < MaxRetries;
                _channel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: requeue);
            }
        }

        private async Task PushBadgeAsync(Guid recipientId, int unreadCount)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory     = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client      = factory.CreateClient("HubService");

                await client.PostAsJsonAsync("/api/notify/badge", new
                {
                    UserId      = recipientId,
                    UnreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Badge push failed for {RecipientId}: {Msg}",
                    recipientId, ex.Message);
            }
        }

        private async Task TrySendEmailAsync(Guid recipientId, string subject, string body)
        {
            try
            {
                var smtpHost  = _config["Email:SmtpHost"];
                var smtpPort  = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser  = _config["Email:SmtpUser"];
                var smtpPass  = _config["Email:SmtpPass"];
                var fromEmail = _config["Email:From"] ?? "noreply@connecthub.com";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser))
                {
                    _logger.LogDebug("SMTP not configured — skipping email for {RecipientId}",
                        recipientId);
                    return;
                }

                var recipientEmail = await GetUserEmailAsync(recipientId);
                if (string.IsNullOrEmpty(recipientEmail)) return;

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(fromEmail));
                email.To.Add(MailboxAddress.Parse(recipientEmail));
                email.Subject = subject;
                email.Body    = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                await smtp.ConnectAsync(smtpHost, smtpPort,
                    MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email sent to {RecipientId}", recipientId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Email failed for {RecipientId}: {Msg}",
                    recipientId, ex.Message);
            }
        }

        private async Task<string?> GetUserEmailAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory     = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client      = factory.CreateClient("AuthService");
                var result = await client
                    .GetFromJsonAsync<UserEmailResponse>($"/api/user/{userId}/email");
                return result?.Email;
            }
            catch
            {
                return null;
            }
        }

        private static int GetDeliveryCount(BasicDeliverEventArgs ea)
        {
            if (ea.BasicProperties?.Headers != null
                && ea.BasicProperties.Headers.TryGetValue("x-delivery-count", out var raw))
                return Convert.ToInt32(raw);
            return 0;
        }

        private void CleanupConnection()
        {
            try { _channel?.Close(); }    catch { /* ignore */ }
            try { _connection?.Close(); } catch { /* ignore */ }
        }

        private class UserEmailResponse { public string Email { get; set; } = ""; }
    }
}