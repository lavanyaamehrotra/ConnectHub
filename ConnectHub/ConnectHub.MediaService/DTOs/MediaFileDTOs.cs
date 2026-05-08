namespace ConnectHub.MediaService.DTOs
{
    // Returned to clients after upload or get
    public class MediaFileDto
    {
        public string   FileId       { get; set; } = "";
        public Guid     UploadedBy   { get; set; }
        public string   FileName     { get; set; } = "";
        public string   ContentType  { get; set; } = "";
        public long     FileSizeKb   { get; set; }
        public string   BlobUrl      { get; set; } = "";
        public string?  ThumbnailUrl { get; set; }
        public int?     MessageId    { get; set; }
        public int?     RoomId       { get; set; }
        public DateTime UploadedAt   { get; set; }
        public DateTime? ExpiresAt   { get; set; }
    }

    // Returned when generating a SAS URL
    public class SasUrlDto
    {
        public string FileId  { get; set; } = "";
        public string SasUrl  { get; set; } = "";
        public DateTime ExpiresAt { get; set; }
    }

    // File stats for admin
    public class FileStatsDto
    {
        public int    TotalFiles     { get; set; }
        public long   TotalSizeKb    { get; set; }
        public int    ImageCount     { get; set; }
        public int    VideoCount     { get; set; }
        public int    DocumentCount  { get; set; }
        public int    ExpiredCount   { get; set; }
    }
}
