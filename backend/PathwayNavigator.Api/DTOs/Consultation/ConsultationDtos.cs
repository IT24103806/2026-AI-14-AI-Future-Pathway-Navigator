using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Consultation;

/// <summary>Created by a student. The student is always the signed-in user, never a body field.</summary>
public class CreateConsultationRequestDto
{
    /// <summary>CareerDiscovery | PathwayPlan | RealityCheck | General</summary>
    [Required, RegularExpression("^(CareerDiscovery|PathwayPlan|RealityCheck|General)$")]
    public string ContextType { get; set; } = "General";

    /// <summary>Id of the analysis / plan / review the student was looking at.</summary>
    public Guid? ContextRefId { get; set; }

    [RegularExpression("^(GuideRequest|DoubtAnswer|RealityCheckClarification|PathwayAdvice|MarketCourseInfo|Unclassified)$")]
    public string? Category { get; set; }

    /// <summary>Optional student-requested urgency. Agent 5 triage may raise it; only a consultant can lower it.</summary>
    [RegularExpression("^(P1|P2|P3)$")]
    public string? Priority { get; set; }

    [Required, StringLength(140, MinimumLength = 4)]
    public string Subject { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Body { get; set; } = string.Empty;
}

public class ConsultationResourceDto
{
    [Required, StringLength(120, MinimumLength = 1)]
    public string Label { get; set; } = string.Empty;

    [Required, StringLength(500, MinimumLength = 4)]
    public string Url { get; set; } = string.Empty;

    /// <summary>guide | course | article | video | tool</summary>
    [StringLength(20)]
    public string Kind { get; set; } = "guide";
}

/// <summary>Communication style hint for the Agent 5 draft reply.</summary>
public enum DraftTone { Supportive, Direct, Detailed }

public class ReplyConsultationDto
{
    [Required, StringLength(4000, MinimumLength = 2)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(10)]
    public List<ConsultationResourceDto> Resources { get; set; } = new();

    /// <summary>
    /// Structured write-back. <c>StageKey</c> attaches the guidance to one roadmap milestone when the
    /// context is a PathwayPlan (null = whole roadmap).
    /// </summary>
    public ConsultationGuidanceDto? Guidance { get; set; }

    [StringLength(1000)]
    public string? ResolutionSummary { get; set; }

    /// <summary>Move the request straight to Closed after this reply (student can still reopen for 7 days).</summary>
    public bool CloseAfterReply { get; set; }

    /// <summary>True when the consultant started from an Agent 5 draft (recorded as an AI-usefulness metric).</summary>
    public bool UsedAgentDraft { get; set; }
}

public class ConsultationGuidanceDto
{
    [StringLength(80)]
    public string? StageKey { get; set; }

    /// <summary>Short paragraph rendered to the student inside the component they asked from.</summary>
    [StringLength(1200)]
    public string? Note { get; set; }

    [MaxLength(10)]
    public List<ConsultationResourceDto> Resources { get; set; } = new();

    [MaxLength(10)]
    public List<string> Checklist { get; set; } = new();

    [MaxLength(10)]
    public List<string> NextSteps { get; set; } = new();
}

public class ConsultationMessageDto
{
    public Guid Id { get; set; }
    public Guid AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public List<ConsultationResourceDto> Resources { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? EditedAt { get; set; }
}

public class ConsultationAuditDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConsultationResponseDto
{
    public Guid Id { get; set; }
    public Guid StudentId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public Guid? AssignedConsultantId { get; set; }
    public string? AssignedConsultantName { get; set; }
    public string ContextType { get; set; } = string.Empty;
    public Guid? ContextRefId { get; set; }
    public string ContextSnapshotJson { get; set; } = "{}";
    /// <summary>Human-readable summary of the frozen context, so both clients render it identically.</summary>
    public string ContextSummary { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime SlaDueAt { get; set; }
    public bool IsSlaBreached { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? AnsweredAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? ResolutionSummary { get; set; }
    public string ConsultantGuidanceJson { get; set; } = "{}";
    public bool AgentDraftUsed { get; set; }
    public int? StudentRating { get; set; }
    public string? StudentFeedback { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ConsultationMessageDto> Messages { get; set; } = new();
    public List<ConsultationAuditDto> AuditEvents { get; set; } = new();
    /// <summary>Agent 5 triage evidence - consultant/admin views only; null for students.</summary>
    public AgentTriageResultDto? AgentTriage { get; set; }
}

public class PagedConsultationsDto
{
    public IReadOnlyList<ConsultationResponseDto> Items { get; set; } = Array.Empty<ConsultationResponseDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class CloseConsultationDto
{
    [Range(1, 5)]
    public int? Rating { get; set; }

    [StringLength(1000)]
    public string? Feedback { get; set; }
}

public class ConsultationContextCheckDto
{
    public bool HasOpenRequest { get; set; }
    public Guid? ConsultationId { get; set; }
    public string? Status { get; set; }
    public string? Subject { get; set; }
}

public class UpdatePriorityDto
{
    [Required, RegularExpression("^(P1|P2|P3)$")]
    public string Priority { get; set; } = "P2";
}

/// <summary>A plain message body - used by the student follow-up endpoint.</summary>
public class ConsultationMessageInputDto
{
    [Required, StringLength(4000, MinimumLength = 2)]
    public string Body { get; set; } = string.Empty;
}

public class InternalNoteDto
{
    [Required, StringLength(4000, MinimumLength = 2)]
    public string Body { get; set; } = string.Empty;
}

// ---------------------------------------------------------------- Agent 5 contracts

public class AgentTriageRequestDto
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    [JsonPropertyName("context_type")] public string ContextType { get; set; } = string.Empty;
    [JsonPropertyName("context_summary")] public string ContextSummary { get; set; } = string.Empty;
    [JsonPropertyName("academic_stage")] public string? AcademicStage { get; set; }
}

public class AgentTriageResultDto
{
    [JsonPropertyName("category")] public string Category { get; set; } = "Unclassified";
    [JsonPropertyName("priority")] public string Priority { get; set; } = "P2";
    [JsonPropertyName("expertise_tags")] public List<string> ExpertiseTags { get; set; } = new();
    [JsonPropertyName("language")] public string Language { get; set; } = "English";
    [JsonPropertyName("sentiment")] public string Sentiment { get; set; } = "neutral";
    [JsonPropertyName("safety_flags")] public List<string> SafetyFlags { get; set; } = new();
    [JsonPropertyName("suggested_sla_hours")] public int SuggestedSlaHours { get; set; } = 48;
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("reasoning")] public string Reasoning { get; set; } = string.Empty;
}

public class AgentBriefRequestDto
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    [JsonPropertyName("context_type")] public string ContextType { get; set; } = string.Empty;
    [JsonPropertyName("context_summary")] public string ContextSummary { get; set; } = string.Empty;
    [JsonPropertyName("student_evidence")] public string StudentEvidenceJson { get; set; } = "{}";
}

public class AgentBriefResultDto
{
    [JsonPropertyName("headline")] public string Headline { get; set; } = string.Empty;
    [JsonPropertyName("sections")] public List<AgentBriefSectionDto> Sections { get; set; } = new();
    [JsonPropertyName("evidence_sources")] public List<string> EvidenceSources { get; set; } = new();
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
}

public class AgentBriefSectionDto
{
    [JsonPropertyName("title")] public string Title { get; set; } = string.Empty;
    [JsonPropertyName("points")] public List<string> Points { get; set; } = new();
}

public class AgentDraftRequestDto
{
    [JsonPropertyName("subject")] public string Subject { get; set; } = string.Empty;
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    [JsonPropertyName("context_type")] public string ContextType { get; set; } = string.Empty;
    [JsonPropertyName("context_summary")] public string ContextSummary { get; set; } = string.Empty;
    [JsonPropertyName("student_evidence")] public string StudentEvidenceJson { get; set; } = "{}";
    [JsonPropertyName("tone")] public string Tone { get; set; } = "Supportive";
}

public class AgentDraftResultDto
{
    [JsonPropertyName("draft_id")] public string DraftId { get; set; } = string.Empty;
    [JsonPropertyName("body")] public string Body { get; set; } = string.Empty;
    [JsonPropertyName("citations")] public List<string> Citations { get; set; } = new();
    [JsonPropertyName("confidence")] public double Confidence { get; set; }
    [JsonPropertyName("safety_notes")] public List<string> SafetyNotes { get; set; } = new();
    [JsonPropertyName("must_escalate")] public bool MustEscalate { get; set; }
}

public class AgentFaqMatchRequestDto
{
    [JsonPropertyName("question")] public string Question { get; set; } = string.Empty;
}

public class AgentFaqMatchResultDto
{
    [JsonPropertyName("matches")] public List<AgentFaqMatchItemDto> Matches { get; set; } = new();
}

public class AgentFaqMatchItemDto
{
    [JsonPropertyName("question")] public string Question { get; set; } = string.Empty;
    [JsonPropertyName("answer")] public string Answer { get; set; } = string.Empty;
    [JsonPropertyName("score")] public double Score { get; set; }
}
