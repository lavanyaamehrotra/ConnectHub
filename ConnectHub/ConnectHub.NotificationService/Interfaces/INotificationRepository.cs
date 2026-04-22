using ConnectHub.NotificationService.Models;

namespace ConnectHub.NotificationService.Interfaces
{
    // ============================================================
    // UC5 — INotificationRepository
    // Matches class diagram methods exactly.
    // FindAllAsync added for admin GetAll (was incorrectly using FindByRecipientAsync(Guid.Empty))
    // ============================================================
    public interface INotificationRepository
    {
        Task<Notification?>            FindByIdAsync(Guid notificationId);
        Task<List<Notification>>       FindUnreadByRecipientAsync(Guid recipientId);
        Task<int>                      CountUnreadByRecipientAsync(Guid recipientId);
        Task<List<Notification>>       FindByRelatedIdAsync(Guid relatedId);
        Task                           MarkAllReadByRecipientAsync(Guid recipientId);
        Task                           DeleteByIdAsync(Guid notificationId);
        Task<Notification>             SaveAsync(Notification notification);
        Task                           UpdateAsync(Notification notification);
        Task<List<Notification>>       FindByRecipientAsync(Guid recipientId, int page = 1, int pageSize = 20);
        Task<List<Notification>>       FindAllAsync(int page = 1, int pageSize = 50);
    }
}