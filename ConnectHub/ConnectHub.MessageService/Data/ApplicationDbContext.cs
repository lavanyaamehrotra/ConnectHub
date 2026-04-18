using Microsoft.EntityFrameworkCore;
using ConnectHub.MessageService.Models;

namespace ConnectHub.MessageService.Data
{
    /// <summary>
    /// 🗄️ DATABASE CONTEXT - The messenger between your app and database
    /// Entity Framework uses this to:
    /// 1. Create database tables
    /// 2. Execute queries (SELECT, INSERT, UPDATE, DELETE)
    /// 3. Track changes to objects
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor - Called when app starts
        /// 'options' contains connection string (how to find PostgreSQL)
        /// base(options) passes settings to parent DbContext class
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// 📋 MESSAGES TABLE - Represents the "Messages" table
        /// DbSet<Message> = EF's representation of the database table
        /// When you write _context.Messages.ToList(), it becomes "SELECT * FROM Messages"
        /// </summary>
        public DbSet<Message> Messages { get; set; }

        /// <summary>
        /// 🎨 CONFIGURE DATABASE MODEL - Extra rules for the database
        /// Called once when the app starts
        /// Used for indexes and constraints
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            /// 🔍 INDEX on SenderId - Speeds up "messages sent by user"
            /// Without index, finding all messages from John would scan EVERY row
            /// With index, it's like a book index - instant lookup!
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.SenderId);

            /// 🔍 INDEX on ReceiverId - Speeds up "messages received by user"
            modelBuilder.Entity<Message>()
                .HasIndex(m => m.ReceiverId);

            /// 🔍 COMPOSITE INDEX - Speeds up conversation queries
            /// This helps queries that filter by BOTH SenderId AND ReceiverId
            /// Example: WHERE SenderId = X AND ReceiverId = Y
            modelBuilder.Entity<Message>()
                .HasIndex(m => new { m.SenderId, m.ReceiverId });
        }
    }
}