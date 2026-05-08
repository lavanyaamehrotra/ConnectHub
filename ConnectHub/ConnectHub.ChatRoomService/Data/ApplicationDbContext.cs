using Microsoft.EntityFrameworkCore;
using ConnectHub.ChatRoomService.Models;

namespace ConnectHub.ChatRoomService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<RoomMember> RoomMembers { get; set; }
        public DbSet<RoomMessage> RoomMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Unique constraint: One user can join a room only once
            modelBuilder.Entity<RoomMember>()
                .HasIndex(m => new { m.RoomId, m.UserId })
                .IsUnique();

            // Indexes for performance
            modelBuilder.Entity<RoomMessage>()
                .HasIndex(m => m.RoomId);

            modelBuilder.Entity<RoomMessage>()
                .HasIndex(m => m.SentAt);

            modelBuilder.Entity<ChatRoom>()
                .HasIndex(r => r.CreatedAt);

            modelBuilder.Entity<ChatRoom>()
                .HasIndex(r => r.RoomType);

            // Global query filter for soft delete
            modelBuilder.Entity<ChatRoom>()
                .HasQueryFilter(r => r.IsActive);

            modelBuilder.Entity<RoomMember>()
                .HasQueryFilter(m => m.IsActive);
        }
    }
}