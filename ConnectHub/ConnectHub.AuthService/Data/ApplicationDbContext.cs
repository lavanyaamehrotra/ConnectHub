using Microsoft.EntityFrameworkCore;
using ConnectHub.AuthService.Models;

namespace ConnectHub.AuthService.Data
{
    /// <summary>
    /// DATABASE CONTEXT - This is the messenger between your app and the database
    /// Think of it as a translator that converts C# code into SQL commands
    /// When you add, update, or delete users, this class does the actual database work
    /// </summary>
    public class ApplicationDbContext : DbContext
    {
        /// <summary>
        /// Constructor - Receives database configuration when the app starts
        /// The options contain connection string (how to find PostgreSQL)
        /// </summary>
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options): base(options){}

        /// <summary>
        /// USERS TABLE - This represents the "Users" table in PostgreSQL
        /// When you write _context.Users.ToList(), it becomes "SELECT * FROM Users"
        /// This is your main way to query user data
        /// </summary>
        public DbSet<User> Users { get; set; }

        /// <summary>
        /// CONFIGURE DATABASE MODEL - Extra rules that can't be put in the User class
        /// Called once when the app starts to set up the database structure
        /// </summary>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // UNIQUE USERNAME - No two people can have the same username
            modelBuilder.Entity<User>().HasIndex(u => u.Username).IsUnique();

            // UNIQUE EMAIL - One email = one account
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // SEARCH INDEX - Makes searching by DisplayName MUCH faster
            modelBuilder.Entity<User>().HasIndex(u => u.DisplayName);

            // ONLINE STATUS INDEX - Makes filtering online users faster
            modelBuilder.Entity<User>().HasIndex(u => u.IsOnline);
        }
    }
}