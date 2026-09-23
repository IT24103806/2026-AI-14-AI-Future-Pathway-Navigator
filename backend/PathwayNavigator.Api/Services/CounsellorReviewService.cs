using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

public class CounsellorReviewService : ICounsellorReviewService
{
    private static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
        { "Pending", "Approved", "Rejected", "NeedsRevision" };

    private readonly AppDbContext _context;
    private readonly IAgentService _agentService;

    public CounsellorReviewService(AppDbContext context, IAgentService agentService)
    {
        _context = context;
        _agentService = agentService;
    }

    public async Task<IReadOnlyList<PathwayReviewResponseDto>> GetPendingReviewsAsync()
    {
        var rows = await _context.PathwayReviews.AsNoTracking()
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        return rows.Select(Map).ToList();
    }

    public async Task<PagedReviewsDto> GetReviewsAsync(string status, string? search, string sort, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var query = _context.PathwayReviews.AsNoTracking().AsQueryable();
        if (!string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!AllowedStatuses.Contains(status)) throw new ArgumentException("Invalid review status.");
            query = query.Where(r => r.Status == status);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(r => r.TargetCareer.ToLower().Contains(term) || r.WorkflowId.ToLower().Contains(term));
        }
        query = sort == "oldest" ? query.OrderBy(r => r.CreatedAt) :
            sort == "score" ? query.OrderBy(r => r.FeasibilityScore) : query.OrderByDescending(r => r.CreatedAt);
        var total = await query.CountAsync();
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedReviewsDto { Items = rows.Select(Map).ToList(), Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<PathwayReviewResponseDto?> GetReviewByIdAsync(Guid id) =>
        (await _context.PathwayReviews.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id)) is { } row ? Map(row) : null;

    public async Task<PathwayReviewResponseDto?> GetReviewByIdForStudentAsync(Guid id, Guid studentId) =>
        (await _context.PathwayReviews.AsNoTracking().SingleOrDefaultAsync(r => r.Id == id && r.StudentId == studentId)) is { } row ? Map(row) : null;

    public async Task<PathwayReviewResponseDto> CreateAsync(Guid analysisId, Guid studentId, StartRealityCheckDto input)
    {
        var ownsAnalysis = await _context.PathwayAnalyses.AnyAsync(a => a.Id == analysisId && a.StudentProfile!.UserId == studentId);
        if (!ownsAnalysis) throw new KeyNotFoundException("Pathway analysis was not found for the signed-in student.");

        var activeExists = await _context.PathwayReviews.AnyAsync(r => r.PathwayAnalysisId == analysisId && r.Status == "Pending");
        if (activeExists) throw new InvalidOperationException("This pathway already has a pending counsellor review.");

        var agentResult = await _agentService.ProcessAgent4RealityCheckAsync(new RealityCheckAgentRequestDto
        {
            StudentId = studentId.ToString(), PathwayAnalysisId = analysisId.ToString(),
            TargetCareer = input.TargetCareer.Trim(), AlStream = input.AlStream.Trim(),
            AlResults = input.AlResults.Trim(), BudgetLevel = input.BudgetLevel,
            CurrentSkills = input.CurrentSkills.Select(s => s.Trim()).Where(s => s.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
        });
        if (agentResult == null) throw new HttpRequestException("Reality Check Agent is temporarily unavailable.");
        if (agentResult.FeasibilityScore is < 0 or > 100 || string.IsNullOrWhiteSpace(agentResult.WorkflowId))
            throw new InvalidDataException("Reality Check Agent returned an invalid structured result.");

        var initialStatus = agentResult.CounsellorReviewRequired ? "Pending" : "Approved";
        var row = new PathwayReview
        {
            PathwayAnalysisId = analysisId, StudentId = studentId, TargetCareer = input.TargetCareer.Trim(),
            WorkflowId = agentResult.WorkflowId, AgentStatus = agentResult.Status,
            Status = initialStatus, IsHighRisk = agentResult.IsHighRisk,
            RiskReason = agentResult.RiskReason, FeasibilityScore = agentResult.FeasibilityScore,
            MissingSkillsJson = JsonSerializer.Serialize(agentResult.MissingSkills),
            FeasibilitySummary = agentResult.SkillGapSummary,
            DegreeRequirement = agentResult.DegreeRequirement,
            SubjectRequirementsJson = JsonSerializer.Serialize(agentResult.SubjectRequirements),
            EntryRequirementsJson = JsonSerializer.Serialize(agentResult.EntryRequirements),
            CostGuidance = agentResult.CostGuidance,
            GapClosurePlanJson = JsonSerializer.Serialize(agentResult.GapClosurePlan),
            EvidenceSourcesJson = JsonSerializer.Serialize(agentResult.EvidenceSources),
            ValidationResultsJson = JsonSerializer.Serialize(agentResult.ValidationResults),
            ToolCallsJson = JsonSerializer.Serialize(agentResult.ToolCalls),
            ExecutionTraceJson = JsonSerializer.Serialize(agentResult.ExecutionTrace),
            AgentError = agentResult.Error, ReviewedAt = initialStatus == "Approved" ? DateTime.UtcNow : null
        };
        row.AuditEvents.Add(new PathwayReviewAudit
        {
            ActorUserId = studentId, Action = "RealityCheckCompleted", FromStatus = "Created", ToStatus = initialStatus,
            Details = agentResult.CounsellorReviewRequired ? "High-impact pathway paused for counsellor approval." : "Deterministic checks passed."
        });
        await using var transaction = await _context.Database.BeginTransactionAsync();
        _context.PathwayReviews.Add(row);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return Map(row);
    }

    public async Task<PathwayReviewResponseDto?> SubmitDecisionAsync(Guid reviewId, Guid counsellorId, CounsellorDecisionDto dto)
    {
        if (!AllowedStatuses.Contains(dto.Decision) || dto.Decision == "Pending")
            throw new ArgumentException("Decision must be Approved, Rejected or NeedsRevision.");
        var review = await _context.PathwayReviews.SingleOrDefaultAsync(r => r.Id == reviewId);
        if (review == null) return null;
        if (review.Status != "Pending") throw new InvalidOperationException("Only pending reviews can receive a decision.");

        var previous = review.Status;
        review.Status = dto.Decision;
        review.CounsellorFeedback = dto.Feedback.Trim();
        review.CounsellorId = counsellorId;
        review.ReviewedAt = DateTime.UtcNow;
        review.UpdatedAt = DateTime.UtcNow;
        _context.PathwayReviewAudits.Add(new PathwayReviewAudit
        {
            PathwayReviewId = review.Id,
            ActorUserId = counsellorId, Action = "CounsellorDecision", FromStatus = previous,
            ToStatus = dto.Decision, Details = review.CounsellorFeedback
        });
        await _context.SaveChangesAsync();
        return Map(review);
    }

    public async Task<PathwayReviewResponseDto?> GetStudentReviewStatusAsync(Guid studentId) =>
        (await _context.PathwayReviews.AsNoTracking().Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt).FirstOrDefaultAsync()) is { } row ? Map(row) : null;

    public async Task<IReadOnlyList<PathwayReviewResponseDto>> GetStudentHistoryAsync(Guid studentId)
    {
        var rows = await _context.PathwayReviews.AsNoTracking().Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt).ToListAsync();
        return rows.Select(Map).ToList();
    }

    private static PathwayReviewResponseDto Map(PathwayReview r) => new()
    {
        Id = r.Id, PathwayAnalysisId = r.PathwayAnalysisId, StudentId = r.StudentId,
        Status = r.Status, IsHighRisk = r.IsHighRisk, RiskReason = r.RiskReason,
        MissingSkillsJson = r.MissingSkillsJson, FeasibilitySummary = r.FeasibilitySummary,
        DegreeRequirement = r.DegreeRequirement, SubjectRequirementsJson = r.SubjectRequirementsJson,
        EntryRequirementsJson = r.EntryRequirementsJson, CostGuidance = r.CostGuidance,
        GapClosurePlanJson = r.GapClosurePlanJson, EvidenceSourcesJson = r.EvidenceSourcesJson,
        FeasibilityScore = r.FeasibilityScore, TargetCareer = r.TargetCareer, WorkflowId = r.WorkflowId,
        AgentStatus = r.AgentStatus, ValidationResultsJson = r.ValidationResultsJson,
        ToolCallsJson = r.ToolCallsJson, ExecutionTraceJson = r.ExecutionTraceJson,
        AgentError = r.AgentError, CounsellorFeedback = r.CounsellorFeedback,
        CreatedAt = r.CreatedAt, UpdatedAt = r.UpdatedAt, ReviewedAt = r.ReviewedAt
    };
}
