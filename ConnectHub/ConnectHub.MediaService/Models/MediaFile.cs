using System.ComponentModel.DataAnnotations;

namespace ConnectHub.MediaService.Models
{
    // ============================================================
    // UC6 — MediaFile Entity
    // Matches class diagram exactly:
    //   FileId, UploadedBy, FileName, ContentType, FileSizeKb,
    //   BlobUrl, ThumbnailUrl, MessageId, RoomId,
    //   UploadedAt, ExpiresAt
    // ============================================================
    public class MediaFile
    {
        [Key]
        public string FileId { get; set; } = Guid.NewGuid().ToString();

        public Guid UploadedBy { get; set; }          // FK to User (AuthService)

        [Required]
        public string FileName { get; set; } = "";

        [Required]
        public string ContentType { get; set; } = ""; // e.g. image/png, application/pdf

        public long FileSizeKb { get; set; }

        [Required]
        public string BlobUrl { get; set; } = "";      // Azure Blob Storage URL

        public string? ThumbnailUrl { get; set; }      // For images only

        public int? MessageId { get; set; }            // Linked message (optional)

        public int? RoomId { get; set; }               // Linked chat room (optional)

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ExpiresAt { get; set; }       // null = never expires
    }
}
