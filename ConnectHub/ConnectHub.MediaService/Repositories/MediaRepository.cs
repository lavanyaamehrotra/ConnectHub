using Microsoft.EntityFrameworkCore;
using ConnectHub.MediaService.Data;
using ConnectHub.MediaService.Interfaces;
using ConnectHub.MediaService.Models;

namespace ConnectHub.MediaService.Repositories
{
    public class MediaRepository : IMediaRepository
    {
        private readonly ApplicationDbContext _db;

        public MediaRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<MediaFile?> FindByFieldId(string fileId)
            => await _db.MediaFiles.FindAsync(fileId);

        public async Task<List<MediaFile>> FindByUploadedBy(Guid userId)
            => await _db.MediaFiles
                .Where(m => m.UploadedBy == userId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

        public async Task<List<MediaFile>> FindByMessageId(int messageId)
            => await _db.MediaFiles
                .Where(m => m.MessageId == messageId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

        public async Task<List<MediaFile>> FindByRoomId(int roomId)
            => await _db.MediaFiles
                .Where(m => m.RoomId == roomId)
                .OrderByDescending(m => m.UploadedAt)
                .ToListAsync();

        public async Task<List<MediaFile>> FindExpiredFiles(DateTime before)
            => await _db.MediaFiles
                .Where(m => m.ExpiresAt != null && m.ExpiresAt < before)
                .ToListAsync();

        public async Task<MediaFile> SaveAsync(MediaFile file)
        {
            _db.MediaFiles.Add(file);
            await _db.SaveChangesAsync();
            return file;
        }

        public async Task DeleteByFileId(string fileId)
        {
            await _db.MediaFiles
                .Where(m => m.FileId == fileId)
                .ExecuteDeleteAsync();
        }

        public async Task<List<MediaFile>> FindAllAsync(int page = 1, int pageSize = 50)
            => await _db.MediaFiles
                .OrderByDescending(m => m.UploadedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
    }
}
