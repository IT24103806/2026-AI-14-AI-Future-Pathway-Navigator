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
        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<PathwayAnalysis> PathwayAnalyses { get; set; }

        public DbSet<PathwayReview> PathwayReviews { get; set; }
        public DbSet<PathwayReviewAudit> PathwayReviewAudits { get; set; }
        public DbSet<GapClosureTask> GapClosureTasks { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Role>().HasData(
                new Role
                {
                    Id = new Guid("10000000-0000-0000-0000-000000000001"),
                    Name = "Student",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = new Guid("10000000-0000-0000-0000-000000000002"),
                    Name = "Counsellor",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Role
                {
                    Id = new Guid("10000000-0000-0000-0000-000000000003"),
                    Name = "Admin",
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                });

            // Enforce unique email constraint
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // 1-to-1 relationship between User and StudentProfile
            modelBuilder.Entity<StudentProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.StudentProfile)
                .HasForeignKey<StudentProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for efficient verification code queries
            modelBuilder.Entity<VerificationCode>()
                .HasIndex(v => new { v.Email, v.Purpose, v.IsUsed });

            // A student profile can have many pathway analyses over time.
            modelBuilder.Entity<PathwayAnalysis>()
                .HasOne(a => a.StudentProfile)
                .WithMany()
                .HasForeignKey(a => a.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);

            // Approver reference is optional and independent of the profile's lifecycle.
            modelBuilder.Entity<PathwayAnalysis>()
                .HasOne(a => a.ApprovedByUser)
                .WithMany()
                .HasForeignKey(a => a.ApprovedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<PathwayReview>()
                .HasIndex(r => r.WorkflowId)
                .IsUnique();
            modelBuilder.Entity<PathwayReview>()
                .HasIndex(r => new { r.Status, r.CreatedAt });
            modelBuilder.Entity<PathwayReview>()
                .HasOne(r => r.PathwayAnalysis)
                .WithMany()
                .HasForeignKey(r => r.PathwayAnalysisId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PathwayReview>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<PathwayReview>()
                .HasMany(r => r.AuditEvents)
                .WithOne(a => a.PathwayReview)
                .HasForeignKey(a => a.PathwayReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GapClosureTask>()
                .HasIndex(t => new { t.StudentId, t.PathwayReviewId });
            modelBuilder.Entity<GapClosureTask>()
                .HasOne(t => t.PathwayReview)
                .WithMany()
                .HasForeignKey(t => t.PathwayReviewId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<GapClosureTask>()
                .HasOne(t => t.Student)
                .WithMany()
                .HasForeignKey(t => t.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
