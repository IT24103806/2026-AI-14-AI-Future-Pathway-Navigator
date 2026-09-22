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

    public string? CounsellorFeedback { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ReviewedAt { get; set; }
}