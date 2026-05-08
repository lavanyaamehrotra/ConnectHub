using ConnectHub.MediaService.Models;

namespace ConnectHub.MediaService.Interfaces
{
    // ============================================================
    // UC6 — IMediaRepository
    // Matches class diagram methods exactly
    // ============================================================
    public interface IMediaRepository
    {
        Task<MediaFile?>           FindByFieldId(string fileId);
        Task<List<MediaFile>>      FindByUploadedBy(Guid userId);
        Task<List<MediaFile>>      FindByMessageId(int messageId);
        Task<List<MediaFile>>      FindByRoomId(int roomId);
        Task<List<MediaFile>>      FindExpiredFiles(DateTime before);
        Task<MediaFile>            SaveAsync(MediaFile file);
        Task                       DeleteByFileId(string fileId);
        Task<List<MediaFile>>      FindAllAsync(int page = 1, int pageSize = 50);
    }
}
