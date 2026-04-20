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

            // Indexes for performance
            modelBuilder.Entity<RoomMember>()
                .HasIndex(m => new { m.RoomId, m.UserId })
                .IsUnique();  // One user can join a room only once

            modelBuilder.Entity<RoomMessage>()
                .HasIndex(m => m.RoomId);

            modelBuilder.Entity<RoomMessage>()
                .HasIndex(m => m.SentAt);

            modelBuilder.Entity<ChatRoom>()
                .HasIndex(r => r.CreatedAt);
        }
    }
}