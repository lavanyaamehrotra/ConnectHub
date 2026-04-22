using ConnectHub.MediaService.DTOs;

namespace ConnectHub.MediaService.Interfaces
{
    // ============================================================
    // UC6 — IMediaService
    // Matches class diagram methods exactly
    // ============================================================
    public interface IMediaService
    {
        Task<MediaFileDto>           UploadFileAsync(IFormFile file, Guid uploadedBy, int? messageId, int? roomId);
        Task<MediaFileDto?>          GetFileByIdAsync(string fileId);
        Task<List<MediaFileDto>>     GetFilesByUserAsync(Guid userId);
        Task<List<MediaFileDto>>     GetFilesByRoomAsync(int roomId);
        Task<List<MediaFileDto>>     GetFilesByMessageAsync(int messageId);
        Task                         DeleteFileAsync(string fileId);
        Task<string>                 GenerateSasUrlAsync(string fileId);
        Task                         CleanupExpiredFilesAsync();
        Task<FileStatsDto>           GetFileStatsAsync();
        Task<Dictionary<string, long>> GetFileSizesAsync(List<string> fileIds);
    }
}
