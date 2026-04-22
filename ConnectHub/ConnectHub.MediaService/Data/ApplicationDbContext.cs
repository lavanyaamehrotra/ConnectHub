using Microsoft.EntityFrameworkCore;
using ConnectHub.MediaService.Models;

namespace ConnectHub.MediaService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<MediaFile> MediaFiles => Set<MediaFile>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index on UploadedBy for fast "get my files" queries
            modelBuilder.Entity<MediaFile>()
                .HasIndex(m => m.UploadedBy);

            // Index on RoomId for fast room file queries
            modelBuilder.Entity<MediaFile>()
                .HasIndex(m => m.RoomId);

            // Index on MessageId for fast message file queries
            modelBuilder.Entity<MediaFile>()
                .HasIndex(m => m.MessageId);

            // Index on ExpiresAt for cleanup job
            modelBuilder.Entity<MediaFile>()
                .HasIndex(m => m.ExpiresAt);
        }
    }
}
