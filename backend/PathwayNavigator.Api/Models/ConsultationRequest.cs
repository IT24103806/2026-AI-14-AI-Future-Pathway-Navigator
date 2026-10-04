using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathwayNavigator.Api.Models
{
    /// <summary>
    /// A student's request for human help, anchored to the exact place in the journey where the
    /// doubt appeared (Career Discovery pathway, roadmap stage, Reality Check review, or general).
    ///
    /// Design notes:
    /// * <see cref="StudentId"/> is always taken from the signed JWT, never from the request body.
    /// * <see cref="ContextSnapshotJson"/> freezes what the student was looking at when they asked, so a
    ///   consultant sees the same numbers even if the underlying analysis is re-run later.
    /// * <see cref="RowVersion"/> is a concurrency token: two consultants cannot claim the same case.
    /// </summary>
    public class ConsultationRequest
    {
        public const string ContextCareerDiscovery = "CareerDiscovery";
        public const string ContextPathwayPlan = "PathwayPlan";
        public const string ContextRealityCheck = "RealityCheck";
        public const string ContextGeneral = "General";

        public const string StatusOpen = "Open";
        public const string StatusClaimed = "Claimed";
        public const string StatusInProgress = "InProgress";
        public const string StatusAwaitingStudent = "AwaitingStudent";
        public const string StatusAnswered = "Answered";
        public const string StatusResolved = "Resolved";
        public const string StatusClosed = "Closed";
        public const string StatusEscalated = "Escalated";

        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid StudentId { get; set; }

        [ForeignKey(nameof(StudentId))]
        public User? Student { get; set; }

        /// <summary>Null while the case sits unclaimed in the consultant pool.</summary>
        public Guid? AssignedConsultantId { get; set; }

        [ForeignKey(nameof(AssignedConsultantId))]
        public User? AssignedConsultant { get; set; }

        /// <summary>CareerDiscovery | PathwayPlan | RealityCheck | General</summary>
        [Required, MaxLength(30)]
        public string ContextType { get; set; } = ContextGeneral;

        /// <summary>PathwayAnalysisId / PathwayPlanId / PathwayReviewId, depending on ContextType.</summary>
        public Guid? ContextRefId { get; set; }

        /// <summary>Frozen JSON copy of the context the student was looking at.</summary>
        public string ContextSnapshotJson { get; set; } = "{}";

        /// <summary>GuideRequest | DoubtAnswer | RealityCheckClarification | PathwayAdvice | MarketCourseInfo | Unclassified</summary>
        [Required, MaxLength(40)]
        public string Category { get; set; } = "Unclassified";

        /// <summary>P1 (blocked, 24h) | P2 (standard, 48h) | P3 (advisory, 72h)</summary>
        [Required, MaxLength(4)]
        public string Priority { get; set; } = "P2";

        [Required, MaxLength(140)]
        public string Subject { get; set; } = string.Empty;

        [Required, MaxLength(4000)]
        public string Body { get; set; } = string.Empty;

        /// <summary>Open | Claimed | InProgress | AwaitingStudent | Answered | Resolved | Closed | Escalated</summary>
        [Required, MaxLength(30)]
        public string Status { get; set; } = StatusOpen;

        public DateTime SlaDueAt { get; set; }

        public DateTime? FirstRespondedAt { get; set; }
        public DateTime? AnsweredAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        [MaxLength(1000)]
        public string? ResolutionSummary { get; set; }

        /// <summary>Structured write-back payload: { guides: [], checklists: [], nextSteps: [] }.</summary>
        public string ConsultantGuidanceJson { get; set; } = "{}";

        /// <summary>Raw Agent 5 triage evidence, retained for audit (never shown to the student).</summary>
        public string? AgentTriageJson { get; set; }

        /// <summary>True when the consultant's reply started from an Agent 5 draft (AI usefulness metric).</summary>
        public bool AgentDraftUsed { get; set; }

        [Range(1, 5)]
        public int? StudentRating { get; set; }

        [MaxLength(1000)]
        public string? StudentFeedback { get; set; }

        /// <summary>
        /// Optimistic-concurrency token. Npgsql maps a <c>uint</c> row version to PostgreSQL's built-in
        /// <c>xmin</c> system column, which changes on every update - so two consultants cannot both win
        /// a claim. Configured explicitly in <see cref="Data.AppDbContext"/>.
        /// </summary>
        [Timestamp]
        public uint RowVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ConsultationMessage> Messages { get; set; } = new List<ConsultationMessage>();
        public ICollection<ConsultationAudit> AuditEvents { get; set; } = new List<ConsultationAudit>();
    }
}
