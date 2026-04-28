using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using ConnectHub.MediaService.DTOs;
using ConnectHub.MediaService.Interfaces;
using ConnectHub.MediaService.Models;

namespace ConnectHub.MediaService.Services
{
    // ============================================================
    // UC6 — MediaService
    //
    // Handles file uploads to Azure Blob Storage.
    // - UploadFileAsync: streams file to Azure, saves metadata to DB
    // - GenerateSasUrlAsync: creates time-limited secure download URL
    // - CleanupExpiredFilesAsync: called by IHostedService daily
    // - GetFileStatsAsync: returns counts/sizes for admin dashboard
    // ============================================================
    public class MediaService : IMediaService
    {
        private readonly IMediaRepository              _repo;
        private readonly BlobServiceClient             _blobClient;
        private readonly IConfiguration                _config;
        private readonly ILogger<MediaService>         _logger;

        private const string ContainerName = "connecthub-media";

        public MediaService(
            IMediaRepository      repo,
            BlobServiceClient     blobClient,
            IConfiguration        config,
            ILogger<MediaService> logger)
        {
            _repo       = repo;
            _blobClient = blobClient;
            _config     = config;
            _logger     = logger;
        }

        // ── UPLOAD ─────────────────────────────────────────────
        public async Task<MediaFileDto> UploadFileAsync(
            IFormFile file,
            Guid      uploadedBy,
            int?      messageId,
            int?      roomId)
        {
            // Validate file
            var maxSizeMb = int.Parse(_config["Azure:MaxFileSizeMb"] ?? "50");
            if (file.Length > maxSizeMb * 1024 * 1024)
                throw new InvalidOperationException($"File exceeds maximum size of {maxSizeMb}MB.");

            var allowedTypes = (_config["Azure:AllowedContentTypes"] ?? "image/,video/,application/pdf,text/")
                .Split(',');
            if (!allowedTypes.Any(t => file.ContentType.StartsWith(t)))
                throw new InvalidOperationException($"File type '{file.ContentType}' is not allowed.");

            // Upload to Azure Blob Storage
            var container = _blobClient.GetBlobContainerClient(ContainerName);
            await container.CreateIfNotExistsAsync(PublicAccessType.Blob);
            // Ensure public access is set even if container already existed
            await container.SetAccessPolicyAsync(PublicAccessType.Blob);

            var blobName   = $"{uploadedBy}/{Guid.NewGuid()}/{file.FileName}";
            var blobClient = container.GetBlobClient(blobName);

            using var stream = file.OpenReadStream();
            await blobClient.UploadAsync(stream, new BlobHttpHeaders
            {
                ContentType = file.ContentType
            });

            var blobUrl = blobClient.Uri.ToString();

            // Generate thumbnail URL for images
            string? thumbnailUrl = null;
            if (file.ContentType.StartsWith("image/"))
                thumbnailUrl = blobUrl; // In production, generate actual thumbnail

            // Save metadata to DB
            var expiresDays = int.Parse(_config["Azure:FileExpiryDays"] ?? "0");
            var mediaFile = new MediaFile
            {
                UploadedBy   = uploadedBy,
                FileName     = file.FileName,
                ContentType  = file.ContentType,
                FileSizeKb   = file.Length / 1024,
                BlobUrl      = blobUrl,
                ThumbnailUrl = thumbnailUrl,
                MessageId    = messageId,
                RoomId       = roomId,
                UploadedAt   = DateTime.UtcNow,
                ExpiresAt    = expiresDays > 0
                    ? DateTime.UtcNow.AddDays(expiresDays)
                    : null
            };

            await _repo.SaveAsync(mediaFile);

            _logger.LogInformation("File {FileName} uploaded by {UserId}, size: {SizeKb}KB",
                file.FileName, uploadedBy, mediaFile.FileSizeKb);

            return ToDto(mediaFile);
        }

        // ── GET BY ID ──────────────────────────────────────────
        public async Task<MediaFileDto?> GetFileByIdAsync(string fileId)
        {
            var file = await _repo.FindByFieldId(fileId);
            return file == null ? null : ToDto(file);
        }

        // ── GET BY USER ────────────────────────────────────────
        public async Task<List<MediaFileDto>> GetFilesByUserAsync(Guid userId)
        {
            var files = await _repo.FindByUploadedBy(userId);
            return files.Select(ToDto).ToList();
        }

        // ── GET BY ROOM ────────────────────────────────────────
        public async Task<List<MediaFileDto>> GetFilesByRoomAsync(int roomId)
        {
            var files = await _repo.FindByRoomId(roomId);
            return files.Select(ToDto).ToList();
        }

        // ── GET BY MESSAGE ─────────────────────────────────────
        public async Task<List<MediaFileDto>> GetFilesByMessageAsync(int messageId)
        {
            var files = await _repo.FindByMessageId(messageId);
            return files.Select(ToDto).ToList();
        }

        // ── DELETE ─────────────────────────────────────────────
        public async Task DeleteFileAsync(string fileId)
        {
            var file = await _repo.FindByFieldId(fileId);
            if (file == null) return;

            // Delete from Azure Blob Storage
            try
            {
                var container  = _blobClient.GetBlobContainerClient(ContainerName);
                var blobName   = new Uri(file.BlobUrl).AbsolutePath
                    .TrimStart('/').Substring(ContainerName.Length + 1);
                var blobClient = container.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not delete blob for {FileId}: {Msg}", fileId, ex.Message);
            }

            await _repo.DeleteByFileId(fileId);
            _logger.LogInformation("File {FileId} deleted", fileId);
        }

        // ── GENERATE SAS URL ───────────────────────────────────
        public async Task<string> GenerateSasUrlAsync(string fileId)
        {
            var file = await _repo.FindByFieldId(fileId);
            if (file == null)
                throw new KeyNotFoundException($"File {fileId} not found.");

            var container  = _blobClient.GetBlobContainerClient(ContainerName);
            var blobName   = new Uri(file.BlobUrl).AbsolutePath
                .TrimStart('/').Substring(ContainerName.Length + 1);
            var blobClient = container.GetBlobClient(blobName);

            // Generate SAS token valid for 1 hour
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = ContainerName,
                BlobName          = blobName,
                Resource          = "b",
                ExpiresOn         = DateTimeOffset.UtcNow.AddHours(1)
            };
            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

            _logger.LogInformation("SAS URL generated for {FileId}", fileId);
            return sasUrl;
        }

        // ── CLEANUP EXPIRED FILES ──────────────────────────────
        public async Task CleanupExpiredFilesAsync()
        {
            var expiredFiles = await _repo.FindExpiredFiles(DateTime.UtcNow);

            _logger.LogInformation("Cleaning up {Count} expired files", expiredFiles.Count);

            foreach (var file in expiredFiles)
                await DeleteFileAsync(file.FileId);
        }

        // ── FILE STATS ─────────────────────────────────────────
        public async Task<FileStatsDto> GetFileStatsAsync()
        {
            var all = await _repo.FindAllAsync(1, int.MaxValue);
            return new FileStatsDto
            {
                TotalFiles    = all.Count,
                TotalSizeKb   = all.Sum(f => f.FileSizeKb),
                ImageCount    = all.Count(f => f.ContentType.StartsWith("image/")),
                VideoCount    = all.Count(f => f.ContentType.StartsWith("video/")),
                DocumentCount = all.Count(f => f.ContentType.StartsWith("application/")),
                ExpiredCount  = all.Count(f => f.ExpiresAt != null && f.ExpiresAt < DateTime.UtcNow)
            };
        }

        // ── GET FILE SIZES ─────────────────────────────────────
        public async Task<Dictionary<string, long>> GetFileSizesAsync(List<string> fileIds)
        {
            var result = new Dictionary<string, long>();
            foreach (var id in fileIds)
            {
                var file = await _repo.FindByFieldId(id);
                if (file != null)
                    result[id] = file.FileSizeKb;
            }
            return result;
        }

        // ── MAP TO DTO ─────────────────────────────────────────
        private static MediaFileDto ToDto(MediaFile f) => new()
        {
            FileId       = f.FileId,
            UploadedBy   = f.UploadedBy,
            FileName     = f.FileName,
            ContentType  = f.ContentType,
            FileSizeKb   = f.FileSizeKb,
            BlobUrl      = f.BlobUrl,
            ThumbnailUrl = f.ThumbnailUrl,
            MessageId    = f.MessageId,
            RoomId       = f.RoomId,
            UploadedAt   = f.UploadedAt,
            ExpiresAt    = f.ExpiresAt
        };
    }
}
