using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Review;

public class StartRealityCheckDto
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string TargetCareer { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 2)]
    public string AlStream { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[ABCFSabcfs](\s*[,/]\s*[ABCFSabcfs])*$")]
    public string AlResults { get; set; } = string.Empty;

    [Required, RegularExpression("^(Low|Medium|High)$")]
    public string BudgetLevel { get; set; } = string.Empty;

    [MaxLength(30)]
    public List<string> CurrentSkills { get; set; } = new();
}

public class RealityCheckAgentRequestDto
{
    [JsonPropertyName("student_id")] public string StudentId { get; set; } = string.Empty;
    [JsonPropertyName("pathway_analysis_id")] public string PathwayAnalysisId { get; set; } = string.Empty;
    [JsonPropertyName("target_career")] public string TargetCareer { get; set; } = string.Empty;
    [JsonPropertyName("al_stream")] public string AlStream { get; set; } = string.Empty;
    [JsonPropertyName("al_results")] public string AlResults { get; set; } = string.Empty;
    [JsonPropertyName("budget_level")] public string BudgetLevel { get; set; } = string.Empty;
    [JsonPropertyName("current_skills")] public List<string> CurrentSkills { get; set; } = new();
}

public class AgentToolCallDto
{
    [JsonPropertyName("tool_name")] public string ToolName { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("duration_ms")] public double DurationMs { get; set; }
    [JsonPropertyName("result_summary")] public string ResultSummary { get; set; } = string.Empty;
}

public class AgentExecutionStepDto
{
    [JsonPropertyName("step")] public string Step { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("duration_ms")] public double DurationMs { get; set; }
}

public class RealityCheckAgentResponseDto
{
    [JsonPropertyName("workflow_id")] public string WorkflowId { get; set; } = string.Empty;
    [JsonPropertyName("status")] public string Status { get; set; } = string.Empty;
    [JsonPropertyName("is_feasible")] public bool IsFeasible { get; set; }
    [JsonPropertyName("feasibility_score")] public int FeasibilityScore { get; set; }
    [JsonPropertyName("missing_skills")] public List<string> MissingSkills { get; set; } = new();
    [JsonPropertyName("skill_gap_summary")] public string SkillGapSummary { get; set; } = string.Empty;
    [JsonPropertyName("degree_requirement")] public string DegreeRequirement { get; set; } = string.Empty;
    [JsonPropertyName("subject_requirements")] public List<string> SubjectRequirements { get; set; } = new();
    [JsonPropertyName("entry_requirements")] public List<string> EntryRequirements { get; set; } = new();
    [JsonPropertyName("cost_guidance")] public string CostGuidance { get; set; } = string.Empty;
    [JsonPropertyName("gap_closure_plan")] public List<string> GapClosurePlan { get; set; } = new();
    [JsonPropertyName("evidence_sources")] public List<string> EvidenceSources { get; set; } = new();
    [JsonPropertyName("is_high_risk")] public bool IsHighRisk { get; set; }
    [JsonPropertyName("risk_reason")] public string? RiskReason { get; set; }
    [JsonPropertyName("counsellor_review_required")] public bool CounsellorReviewRequired { get; set; }
    [JsonPropertyName("suggested_action")] public string SuggestedAction { get; set; } = string.Empty;
    [JsonPropertyName("validation_results")] public List<string> ValidationResults { get; set; } = new();
    [JsonPropertyName("tool_calls")] public List<AgentToolCallDto> ToolCalls { get; set; } = new();
    [JsonPropertyName("execution_trace")] public List<AgentExecutionStepDto> ExecutionTrace { get; set; } = new();
    [JsonPropertyName("error")] public string? Error { get; set; }
}

public class PagedReviewsDto
{
    public IReadOnlyList<PathwayReviewResponseDto> Items { get; set; } = Array.Empty<PathwayReviewResponseDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
