using Microsoft.EntityFrameworkCore;
using StudioRental_Markov.Models;

namespace StudioRental_Markov.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Studio> Studios { get; set; }
        public DbSet<Booking> Bookings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Уникальный email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Индекс для поиска студий по владельцу
            modelBuilder.Entity<Studio>()
                .HasIndex(s => s.OwnerId);

            // Индекс для поиска бронирований по студии и датам
            modelBuilder.Entity<Booking>()
                .HasIndex(b => new { b.StudioId, b.StartTime, b.EndTime });

            // Индекс для поиска бронирований пользователя
            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.CustomerId);
        }
    }
}