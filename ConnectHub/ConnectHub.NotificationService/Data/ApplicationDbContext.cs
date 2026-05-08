using Microsoft.EntityFrameworkCore;
using ConnectHub.NotificationService.Models;

namespace ConnectHub.NotificationService.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Notification> Notifications => Set<Notification>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Index on RecipientId for fast "get my notifications" queries
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.RecipientId);

            // Index on IsRead for fast unread count queries
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.RecipientId, n.IsRead });
        }
    }
}
