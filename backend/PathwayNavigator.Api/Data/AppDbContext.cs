using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<VerificationCode> VerificationCodes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Enforce unique email constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Index for efficient verification code queries
            modelBuilder.Entity<VerificationCode>()
                .HasIndex(v => new { v.Email, v.Purpose, v.IsUsed });
        }
    }
}