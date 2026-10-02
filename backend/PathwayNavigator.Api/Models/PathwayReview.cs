using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathwayNavigator.Api.Models;

public class PathwayReview
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid PathwayAnalysisId { get; set; }

    [ForeignKey(nameof(PathwayAnalysisId))]
    public PathwayAnalysis? PathwayAnalysis { get; set; }

    [Required]
    public Guid StudentId { get; set; }

    [ForeignKey(nameof(StudentId))]
    public User? Student { get; set; }

    public Guid? CounsellorId { get; set; }

    [ForeignKey(nameof(CounsellorId))]
    public User? Counsellor { get; set; }

    // Status: Pending, Approved, Rejected, NeedsRevision
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";

    public bool IsHighRisk { get; set; } = false;

    public string? RiskReason { get; set; }

    public string? MissingSkillsJson { get; set; }

    public string? FeasibilitySummary { get; set; }
    public string DegreeRequirement { get; set; } = string.Empty;
    public string SubjectRequirementsJson { get; set; } = "[]";
    public string EntryRequirementsJson { get; set; } = "[]";
    public string CostGuidance { get; set; } = string.Empty;
    public string GapClosurePlanJson { get; set; } = "[]";
    public string EvidenceSourcesJson { get; set; } = "[]";

    [Range(0, 100)]
    public int FeasibilityScore { get; set; }

    [Required, MaxLength(120)]
    public string TargetCareer { get; set; } = string.Empty;

    [MaxLength(80)]
    public string AlStream { get; set; } = string.Empty;

    [MaxLength(80)]
    public string AlResults { get; set; } = string.Empty;

    [MaxLength(20)]
    public string BudgetLevel { get; set; } = "Medium";

    public string CurrentSkillsJson { get; set; } = "[]";

    [Required, MaxLength(100)]
    public string WorkflowId { get; set; } = string.Empty;

    [Required, MaxLength(40)]
    public string AgentStatus { get; set; } = "approval_required";

    public string ValidationResultsJson { get; set; } = "[]";
    public string ToolCallsJson { get; set; } = "[]";
    public string ExecutionTraceJson { get; set; } = "[]";
    public string? AgentError { get; set; }

    public string? CounsellorFeedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }

    public ICollection<PathwayReviewAudit> AuditEvents { get; set; } = new List<PathwayReviewAudit>();
}
