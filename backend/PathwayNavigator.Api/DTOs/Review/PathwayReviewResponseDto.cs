namespace PathwayNavigator.Api.DTOs.Review;

public class PathwayReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid PathwayAnalysisId { get; set; }
    public Guid StudentId { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsHighRisk { get; set; }
    public string? RiskReason { get; set; }
    public string? MissingSkillsJson { get; set; }
    public string? FeasibilitySummary { get; set; }
    public string? CounsellorFeedback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}