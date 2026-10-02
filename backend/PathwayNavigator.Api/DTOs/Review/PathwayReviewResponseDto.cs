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
    public string DegreeRequirement { get; set; } = string.Empty;
    public string SubjectRequirementsJson { get; set; } = "[]";
    public string EntryRequirementsJson { get; set; } = "[]";
    public string CostGuidance { get; set; } = string.Empty;
    public string GapClosurePlanJson { get; set; } = "[]";
    public string EvidenceSourcesJson { get; set; } = "[]";
    public int FeasibilityScore { get; set; }
    public string TargetCareer { get; set; } = string.Empty;
    public string AlStream { get; set; } = string.Empty;
    public string AlResults { get; set; } = string.Empty;
    public string BudgetLevel { get; set; } = "Medium";
    public string CurrentSkillsJson { get; set; } = "[]";
    public string WorkflowId { get; set; } = string.Empty;
    public string AgentStatus { get; set; } = string.Empty;
    public string ValidationResultsJson { get; set; } = "[]";
    public string ToolCallsJson { get; set; } = "[]";
    public string ExecutionTraceJson { get; set; } = "[]";
    public string? AgentError { get; set; }
    public string? CounsellorFeedback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
}
