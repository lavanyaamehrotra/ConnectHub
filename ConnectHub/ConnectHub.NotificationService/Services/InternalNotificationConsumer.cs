using System.Threading.Channels;
using ConnectHub.NotificationService.Messaging;
using MailKit.Net.Smtp;
using MimeKit;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConnectHub.NotificationService.Services
{
    public class InternalNotificationConsumer : BackgroundService
    {
        private readonly Channel<NotificationEvent> _channel;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;
        private readonly ILogger<InternalNotificationConsumer> _logger;

        public InternalNotificationConsumer(
            Channel<NotificationEvent> channel,
            IServiceScopeFactory scopeFactory,
            IConfiguration config,
            ILogger<InternalNotificationConsumer> logger)
        {
            _channel = channel;
            _scopeFactory = scopeFactory;
            _config = config;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("InternalNotificationConsumer started. Waiting for events...");

            await foreach (var evt in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    _logger.LogInformation("Processing in-memory notification {Id} for user {UserId}",
                        evt.NotificationId, evt.RecipientId);

                    // 1. Badge push
                    await PushBadgeAsync(evt.RecipientId, evt.UnreadCount);

                    // 2. Fetch sender info
                    string senderName = "Someone";
                    if (evt.SenderId.HasValue)
                    {
                        var senderInfo = await GetUserInfoAsync(evt.SenderId.Value);
                        senderName = senderInfo?.DisplayName ?? senderInfo?.Username ?? "Someone";
                    }

                    // 3. Send email
                    var emailSubject = $"{senderName} messaged you on ConnectHub";
                    var emailBody = $"Hi! {senderName} sent you a message: \"{evt.Message}\"\n\nLog in to ConnectHub to reply.";
                    
                    await TrySendEmailAsync(evt.RecipientId, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing in-memory notification {Id}", evt.NotificationId);
                }
            }
        }

        private async Task PushBadgeAsync(Guid recipientId, int unreadCount)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("HubService");

                await client.PostAsJsonAsync("/api/notify/badge", new
                {
                    UserId = recipientId,
                    UnreadCount = unreadCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Badge push failed for {RecipientId}: {Msg}", recipientId, ex.Message);
            }
        }

        private async Task TrySendEmailAsync(Guid recipientId, string subject, string body)
        {
            try
            {
                var smtpHost = _config["Email:SmtpHost"];
                var smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
                var smtpUser = _config["Email:SmtpUser"];
                var smtpPass = _config["Email:SmtpPass"]?.Trim().Replace(" ", "");
                var fromEmail = _config["Email:From"] ?? smtpUser ?? "noreply@connecthub.com";

                if (string.IsNullOrEmpty(smtpHost) || string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPass))
                {
                    _logger.LogWarning("SMTP not configured — skipping email for {RecipientId}", recipientId);
                    return;
                }

                var recipientEmail = await GetUserEmailAsync(recipientId);
                if (string.IsNullOrEmpty(recipientEmail))
                {
                    _logger.LogWarning("Could not find email for user {RecipientId}", recipientId);
                    return;
                }

                var email = new MimeMessage();
                email.From.Add(MailboxAddress.Parse(fromEmail));
                email.To.Add(MailboxAddress.Parse(recipientEmail));
                email.Subject = subject;
                email.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("Email successfully sent to {Email}", recipientEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Email dispatch failed for {RecipientId}", recipientId);
            }
        }

        private async Task<UserResponse?> GetUserInfoAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("AuthService");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return await client.GetFromJsonAsync<UserResponse>($"/api/user/{userId}", options);
            }
            catch { return null; }
        }

        private async Task<string?> GetUserEmailAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("AuthService");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = await client.GetFromJsonAsync<UserEmailResponse>($"/api/user/{userId}/email", options);
                return result?.Email;
            }
            catch { return null; }
        }

        private class UserResponse 
        { 
            [JsonPropertyName("username")] public string Username { get; set; } = ""; 
            [JsonPropertyName("displayName")] public string DisplayName { get; set; } = ""; 
        }

        private class UserEmailResponse 
        { 
            [JsonPropertyName("email")] public string Email { get; set; } = ""; 
        }
    }
}
