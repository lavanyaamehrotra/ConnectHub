using System;
using System.Threading.Tasks;

namespace ConnectHub.HubService.Interfaces
{
    public interface INotificationServiceClient
    {
        Task SendNotificationAsync(Guid recipientId, Guid senderId, string type, string title, string message, string? relatedId = null, string? relatedType = null, string? token = null);
    }
}
