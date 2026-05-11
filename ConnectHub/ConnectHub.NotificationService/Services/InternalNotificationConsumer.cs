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
                    _logger.LogInformation(">>> PROCESSING NOTIFICATION {Id} for User {UserId}", evt.NotificationId, evt.RecipientId);

                    // 1. Badge push (SignalR)
                    await PushBadgeAsync(evt.RecipientId, evt.UnreadCount);

                    // 2. Email Notification
                    // We only send email for MESSAGE and ROOM_INVITE types by default, or you can send for all
                    var emailSubject = $"ConnectHub: {evt.Title}";
                    
                    // Fetch sender name for better context
                    string senderName = "Someone";
                    if (evt.SenderId.HasValue)
                    {
                        var senderInfo = await GetUserInfoAsync(evt.SenderId.Value);
                        if (senderInfo != null)
                        {
                            senderName = !string.IsNullOrEmpty(senderInfo.DisplayName) ? senderInfo.DisplayName : senderInfo.Username;
                        }
                    }

                    var emailBody = $"Hi!\n\n{senderName} sent you a message: \"{evt.Message}\"\n\nLog in to ConnectHub to reply.\nhttps://connecthub-frontend-f8dq.onrender.com";
                    
                    await TrySendEmailAsync(evt.RecipientId, emailSubject, emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "CRITICAL ERROR in InternalNotificationConsumer loop for {Id}", evt.NotificationId);
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

                _logger.LogInformation("Pushing badge count {Count} to HubService for {UserId}...", unreadCount, recipientId);
                var response = await client.PostAsJsonAsync("/api/notify/badge", new
                {
                    UserId = recipientId,
                    UnreadCount = unreadCount
                });

                if (response.IsSuccessStatusCode)
                    _logger.LogInformation("Badge push successful.");
                else
                    _logger.LogWarning("Badge push failed: {Status}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Badge push exception: {Msg}", ex.Message);
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
                    _logger.LogWarning("SMTP NOT CONFIGURED in appsettings. Skipping email.");
                    return;
                }

                _logger.LogInformation("Attempting to send email to user {UserId}...", recipientId);

                var recipientEmail = await GetUserEmailAsync(recipientId);
                if (string.IsNullOrEmpty(recipientEmail))
                {
                    _logger.LogWarning("ABORT: Could not retrieve email for user {UserId} from AuthService.", recipientId);
                    return;
                }

                _logger.LogInformation("Recipient email found: {Email}. Connecting to SMTP {Host}:{Port}...", recipientEmail, smtpHost, smtpPort);

                var email = new MimeMessage();
                email.From.Add(new MailboxAddress("ConnectHub", fromEmail));
                email.To.Add(MailboxAddress.Parse(recipientEmail));
                email.Subject = subject;
                email.Body = new TextPart("plain") { Text = body };

                using var smtp = new SmtpClient();
                // Bypass cert validation for Render/Cloud environments to prevent SSL handshake errors
                smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;
                
                await smtp.ConnectAsync(smtpHost, smtpPort, MailKit.Security.SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(smtpUser, smtpPass);
                await smtp.SendAsync(email);
                await smtp.DisconnectAsync(true);

                _logger.LogInformation("SUCCESS: Email sent to {Email} via {Host}", recipientEmail, smtpHost);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "FAILED to send email to user {UserId}", recipientId);
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
                
                _logger.LogInformation("Fetching user info from {Url}...", client.BaseAddress + $"api/user/{userId}");
                return await client.GetFromJsonAsync<UserResponse>($"/api/user/{userId}", options);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Failed to fetch user info: {Msg}", ex.Message);
                return null;
            }
        }

        private async Task<string?> GetUserEmailAsync(Guid userId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var factory = scope.ServiceProvider.GetRequiredService<IHttpClientFactory>();
                var client = factory.CreateClient("AuthService");
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                
                var url = $"/api/user/{userId}/email";
                _logger.LogInformation("Fetching user email from {FullUrl}...", client.BaseAddress + url.TrimStart('/'));
                
                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("AuthService returned {Status} for email lookup: {Error}", response.StatusCode, error);
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<UserEmailResponse>(options);
                return result?.Email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EXCEPTION during email lookup for user {UserId}", userId);
                return null;
            }
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
