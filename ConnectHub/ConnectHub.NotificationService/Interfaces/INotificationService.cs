using ConnectHub.NotificationService.DTOs;
using ConnectHub.NotificationService.Models;

namespace ConnectHub.NotificationService.Interfaces
{
    // ============================================================
    // UC5 — INotificationService
    // Matches class diagram: Send, SendBulk, GetByRecipient,
    // GetUnread, GetUnreadCount, MarkAsRead, MarkAllRead,
    // DeleteNotification, SendEmail, GetAll, FindByType
    // ============================================================
    public interface INotificationService
    {
        Task<NotificationDto>       SendAsync(CreateNotificationDto dto);
        Task                        SendBulkAsync(BulkNotificationDto dto);
        Task<List<NotificationDto>> GetByRecipientAsync(Guid recipientId, int page = 1, int pageSize = 20);
        Task<List<NotificationDto>> GetUnreadAsync(Guid recipientId);
        Task<int>                   GetUnreadCountAsync(Guid recipientId);
        Task                        MarkAsReadAsync(Guid notificationId);
        Task                        MarkAllReadAsync(Guid recipientId);
        Task                        DeleteNotificationAsync(Guid notificationId);
        Task<List<NotificationDto>> GetAllAsync(int page = 1, int pageSize = 50);
        Task<List<NotificationDto>> FindByTypeAsync(string type);
    }
}
