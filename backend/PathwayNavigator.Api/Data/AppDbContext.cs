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
        public DbSet<PathwayPlan> PathwayPlans { get; set; }

        public DbSet<PathwayReview> PathwayReviews { get; set; }
        public DbSet<PathwayReviewAudit> PathwayReviewAudits { get; set; }

        // Consultant support layer (role `Consultant`, consultation threads, notification inbox)
        public DbSet<ConsultantProfile> ConsultantProfiles { get; set; }
        public DbSet<ConsultationRequest> ConsultationRequests { get; set; }
        public DbSet<ConsultationMessage> ConsultationMessages { get; set; }
        public DbSet<ConsultationAudit> ConsultationAudits { get; set; }
        public DbSet<Notification> Notifications { get; set; }

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
                },
                new Role
                {
                    Id = new Guid("10000000-0000-0000-0000-000000000004"),
                    Name = "Consultant",
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

            // A student profile can have persisted Agent 3 pathway plans with stage progress.
            modelBuilder.Entity<PathwayPlan>()
                .HasOne(p => p.StudentProfile)
                .WithMany()
                .HasForeignKey(p => p.StudentProfileId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<PathwayPlan>()
                .HasIndex(p => new { p.StudentProfileId, p.UpdatedAt });
            modelBuilder.Entity<PathwayPlan>()
                .HasIndex(p => new { p.StudentProfileId, p.SelectedPathway });

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

            // ---------------------------------------------------------------- Consultant support layer

            // 1-to-1 between User and ConsultantProfile (only consultant accounts have a row).
            modelBuilder.Entity<ConsultantProfile>()
                .HasOne(p => p.User)
                .WithOne(u => u.ConsultantProfile)
                .HasForeignKey<ConsultantProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Optimistic concurrency: makes a simultaneous double-claim impossible (the loser gets a
            // DbUpdateConcurrencyException, which the queue service turns into a 409).
            modelBuilder.Entity<ConsultationRequest>()
                .Property(r => r.RowVersion)
                .IsRowVersion();

            // The queue is read as "unclaimed pool" and "my cases", always ordered by urgency.
            modelBuilder.Entity<ConsultationRequest>()
                .HasIndex(r => new { r.Status, r.Priority, r.CreatedAt });
            modelBuilder.Entity<ConsultationRequest>()
                .HasIndex(r => new { r.StudentId, r.CreatedAt });
            modelBuilder.Entity<ConsultationRequest>()
                .HasIndex(r => new { r.AssignedConsultantId, r.Status });
            modelBuilder.Entity<ConsultationRequest>()
                .HasIndex(r => new { r.ContextType, r.ContextRefId });

            modelBuilder.Entity<ConsultationRequest>()
                .HasOne(r => r.Student)
                .WithMany()
                .HasForeignKey(r => r.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ConsultationRequest>()
                .HasOne(r => r.AssignedConsultant)
                .WithMany()
                .HasForeignKey(r => r.AssignedConsultantId)
                .OnDelete(DeleteBehavior.SetNull);
            modelBuilder.Entity<ConsultationRequest>()
                .HasMany(r => r.Messages)
                .WithOne(m => m.ConsultationRequest)
                .HasForeignKey(m => m.ConsultationRequestId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<ConsultationRequest>()
                .HasMany(r => r.AuditEvents)
                .WithOne(a => a.ConsultationRequest)
                .HasForeignKey(a => a.ConsultationRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ConsultationMessage>()
                .Property(m => m.Id)
                .ValueGeneratedNever();
            modelBuilder.Entity<ConsultationMessage>()
                .HasIndex(m => new { m.ConsultationRequestId, m.CreatedAt });
            modelBuilder.Entity<ConsultationMessage>()
                .HasOne(m => m.Author)
                .WithMany()
                .HasForeignKey(m => m.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ConsultationAudit>()
                .Property(a => a.Id)
                .ValueGeneratedNever();
            modelBuilder.Entity<ConsultationAudit>()
                .HasIndex(a => new { a.ConsultationRequestId, a.CreatedAt });
            modelBuilder.Entity<ConsultationAudit>()
                .HasOne(a => a.Actor)
                .WithMany()
                .HasForeignKey(a => a.ActorUserId)
                .OnDelete(DeleteBehavior.SetNull);

            // The unread badge is a single indexed seek on (UserId, IsRead, CreatedAt).
            modelBuilder.Entity<Notification>()
                .HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt });
            // A recurring event (SLA breach) must not create duplicate rows for the same condition.
            modelBuilder.Entity<Notification>()
                .HasIndex(n => n.DedupeKey)
                .IsUnique()
                .HasFilter("\"DedupeKey\" IS NOT NULL");
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
