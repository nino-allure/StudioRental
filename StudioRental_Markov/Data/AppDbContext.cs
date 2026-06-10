using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Studio> Studios { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<SystemLog> SystemLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Studio>()
                .HasIndex(s => s.OwnerId);

            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.StudioId, b.StartTime, b.EndTime });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.CustomerId);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => l.CreatedAt);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => l.LogLevel);

            modelBuilder.Entity<SystemLog>()
                .HasIndex(l => l.Category);
        }
    }
}