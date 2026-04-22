using Microsoft.EntityFrameworkCore;
using ConnectHub.NotificationService.Data;
using ConnectHub.NotificationService.Interfaces;
using ConnectHub.NotificationService.Models;

namespace ConnectHub.NotificationService.Repositories
{
    // ============================================================
    // UC5 — NotificationRepository
    //
    // FIXES vs original:
    //   1. Added UpdateAsync(Notification) — used by MarkAsReadAsync
    //      so we don't call SaveAsync (Add) on an existing entity.
    //   2. Added FindAllAsync(page, pageSize) — admin GetAll query
    //      (original incorrectly passed Guid.Empty to FindByRecipientAsync)
    // ============================================================
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _db;

        public NotificationRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<Notification?> FindByIdAsync(Guid notificationId)
            => await _db.Notifications.FindAsync(notificationId);

        public async Task<List<Notification>> FindUnreadByRecipientAsync(Guid recipientId)
            => await _db.Notifications
                .Where(n => n.RecipientId == recipientId && !n.IsRead)
                .OrderByDescending(n => n.SentAt)
                .ToListAsync();

        public async Task<int> CountUnreadByRecipientAsync(Guid recipientId)
            => await _db.Notifications
                .CountAsync(n => n.RecipientId == recipientId && !n.IsRead);

        public async Task<List<Notification>> FindByRelatedIdAsync(Guid relatedId)
            => await _db.Notifications
                .Where(n => n.RelatedId == relatedId)
                .ToListAsync();

        public async Task MarkAllReadByRecipientAsync(Guid recipientId)
        {
            await _db.Notifications
                .Where(n => n.RecipientId == recipientId && !n.IsRead)
                .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
        }

        public async Task DeleteByIdAsync(Guid notificationId)
        {
            await _db.Notifications
                .Where(n => n.NotificationId == notificationId)
                .ExecuteDeleteAsync();
        }

        // INSERT — for new notifications only
        public async Task<Notification> SaveAsync(Notification notification)
        {
            _db.Notifications.Add(notification);
            await _db.SaveChangesAsync();
            return notification;
        }

        // UPDATE — for marking read/unread on existing tracked entities
        public async Task UpdateAsync(Notification notification)
        {
            _db.Notifications.Update(notification);
            await _db.SaveChangesAsync();
        }

        public async Task<List<Notification>> FindByRecipientAsync(Guid recipientId, int page = 1, int pageSize = 20)
            => await _db.Notifications
                .Where(n => n.RecipientId == recipientId)
                .OrderByDescending(n => n.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        // FIX: Admin endpoint — returns ALL notifications across all users
        public async Task<List<Notification>> FindAllAsync(int page = 1, int pageSize = 50)
            => await _db.Notifications
                .OrderByDescending(n => n.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
    }
}