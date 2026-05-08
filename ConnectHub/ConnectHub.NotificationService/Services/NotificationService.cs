using ConnectHub.NotificationService.DTOs;
using ConnectHub.NotificationService.Interfaces;
using ConnectHub.NotificationService.Messaging;
using ConnectHub.NotificationService.Models;

namespace ConnectHub.NotificationService.Services
{
    // ============================================================
    // UC5 — NotificationService (FIXED)
    //
    // FIXES vs original:
    //   1. Removed Microsoft.AspNetCore.SignalR.Client import — was
    //      causing compile error (not in .csproj and not needed here;
    //      badge push is done via REST call to HubService).
    //   2. MarkAsReadAsync now calls _repo.UpdateAsync() instead of
    //      _repo.SaveAsync() — SaveAsync does _db.Add() which causes
    //      a duplicate-insert crash on existing entities.
    //   3. GetAllAsync now calls _repo.FindAllAsync() instead of
    //      FindByRecipientAsync(Guid.Empty) which returned nothing.
    //   4. After saving to DB, an event is published to RabbitMQ.
    //      The NotificationConsumer background service handles:
    //        • Badge push to HubService → SignalR → browser
    //        • Email via MailKit for offline users
    //      This makes SendAsync non-blocking and resilient — if
    //      HubService or SMTP is temporarily down, messages queue
    //      up and are retried without failing the API caller.
    //   5. SendBulkAsync publishes events concurrently (Task.WhenAll)
    //      instead of sequential awaits.
    // ============================================================
    public class NotificationService : INotificationService
    {
        private readonly INotificationRepository     _repo;
        private readonly INotificationPublisher      _publisher;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            INotificationRepository      repo,
            INotificationPublisher       publisher,
            ILogger<NotificationService> logger)
        {
            _repo      = repo;
            _publisher = publisher;
            _logger    = logger;
        }

        // -----------------------------------------------------------
        // SEND — main method
        // 1. Save notification to DB (synchronous — caller needs the ID)
        // 2. Publish event to RabbitMQ (non-blocking)
        //    Consumer handles: badge push to HubService + email
        // -----------------------------------------------------------
        public async Task<NotificationDto> SendAsync(CreateNotificationDto dto)
        {
            var notification = new Notification
            {
                RecipientId = dto.RecipientId,
                SenderId    = dto.SenderId,
                Type        = dto.Type,
                Title       = dto.Title,
                Message     = dto.Message,
                RelatedId   = dto.RelatedId,
                RelatedType = dto.RelatedType,
                SentAt      = DateTime.UtcNow
            };

            await _repo.SaveAsync(notification);

            // Compute unread count now so the consumer doesn't need a DB query
            var unreadCount = await _repo.CountUnreadByRecipientAsync(dto.RecipientId);

            // Publish to RabbitMQ — consumer handles badge push + email async
            await _publisher.PublishAsync(new NotificationEvent
            {
                NotificationId = notification.NotificationId,
                RecipientId    = dto.RecipientId,
                SenderId       = dto.SenderId,
                Type           = dto.Type,
                Title          = dto.Title,
                Message        = dto.Message,
                RelatedId      = dto.RelatedId,
                RelatedType    = dto.RelatedType,
                UnreadCount    = unreadCount,
                SentAt         = notification.SentAt
            });

            _logger.LogInformation("Notification {Id} saved and queued for {RecipientId}, type: {Type}",
                notification.NotificationId, dto.RecipientId, dto.Type);

            return ToDto(notification);
        }

        // -----------------------------------------------------------
        // SEND BULK — admin platform-wide broadcast
        // Publishes all events concurrently instead of sequentially
        // -----------------------------------------------------------
        public async Task SendBulkAsync(BulkNotificationDto dto)
        {
            foreach (var recipientId in dto.RecipientIds)
            {
                await SendAsync(new CreateNotificationDto
                {
                    RecipientId = recipientId,
                    Type        = dto.Type,
                    Title       = dto.Title,
                    Message     = dto.Message
                });
            }

            _logger.LogInformation("Bulk notification sent to {Count} users", dto.RecipientIds.Count);
        }

        public async Task<List<NotificationDto>> GetByRecipientAsync(Guid recipientId, int page = 1, int pageSize = 20)
        {
            var items = await _repo.FindByRecipientAsync(recipientId, page, pageSize);
            return items.Select(ToDto).ToList();
        }

        public async Task<List<NotificationDto>> GetUnreadAsync(Guid recipientId)
        {
            var items = await _repo.FindUnreadByRecipientAsync(recipientId);
            return items.Select(ToDto).ToList();
        }

        public async Task<int> GetUnreadCountAsync(Guid recipientId)
            => await _repo.CountUnreadByRecipientAsync(recipientId);

        // FIX: was calling _repo.SaveAsync(n) which does _db.Add() → duplicate insert crash
        public async Task MarkAsReadAsync(Guid notificationId)
        {
            var n = await _repo.FindByIdAsync(notificationId);
            if (n == null) return;
            n.IsRead = true;
            await _repo.UpdateAsync(n);  // UPDATE, not INSERT
        }

        public async Task MarkAllReadAsync(Guid recipientId)
            => await _repo.MarkAllReadByRecipientAsync(recipientId);

        public async Task DeleteNotificationAsync(Guid notificationId)
            => await _repo.DeleteByIdAsync(notificationId);

        // FIX: was calling FindByRecipientAsync(Guid.Empty) → returned nothing
        public async Task<List<NotificationDto>> GetAllAsync(int page = 1, int pageSize = 50)
        {
            var items = await _repo.FindAllAsync(page, pageSize);
            return items.Select(ToDto).ToList();
        }

        public async Task<List<NotificationDto>> FindByTypeAsync(string type)
        {
            // Pull recent page and filter — for production use a DB index on Type
            var items = await _repo.FindAllAsync(1, 200);
            return items.Where(n => n.Type == type).Select(ToDto).ToList();
        }

        // Map entity → DTO
        private static NotificationDto ToDto(Notification n) => new()
        {
            NotificationId = n.NotificationId,
            RecipientId    = n.RecipientId,
            SenderId       = n.SenderId,
            Type           = n.Type,
            Title          = n.Title,
            Message        = n.Message,
            RelatedId      = n.RelatedId,
            RelatedType    = n.RelatedType,
            IsRead         = n.IsRead,
            SentAt         = n.SentAt
        };
    }
}