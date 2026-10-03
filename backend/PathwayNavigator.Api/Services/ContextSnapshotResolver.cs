using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

public record ContextSnapshot(
    string ContextType,
    Guid? ContextRefId,
    string Summary,
    string Json,
    /// <summary>Agent 1/2/3/4 evidence handed to Agent 5 - consultant/admin eyes only.</summary>
    string StudentEvidenceJson);

/// <summary>
/// Turns a <c>contextType</c> + <c>contextRefId</c> into a frozen, human-readable snapshot.
///
/// Two responsibilities, deliberately in one place:
/// 1. <b>Authorization</b> - the referenced analysis/plan/review must belong to the signed-in student,
///    so a crafted request cannot make a consultant look at someone else's data.
/// 2. <b>Context freezing</b> - the snapshot is stored on the request, so the consultant sees the
///    same numbers the student saw even if the pathway is re-analysed later.
/// </summary>
public interface IContextSnapshotResolver
{
    Task<ContextSnapshot> ResolveAsync(Guid studentId, string contextType, Guid? contextRefId);
}

public class ContextSnapshotResolver : IContextSnapshotResolver
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };

    private readonly AppDbContext _context;

    public ContextSnapshotResolver(AppDbContext context) => _context = context;

    public async Task<ContextSnapshot> ResolveAsync(Guid studentId, string contextType, Guid? contextRefId)
    {
        var normalized = (contextType ?? ConsultationRequest.ContextGeneral).Trim();

        try
        {
            return normalized switch
            {
                ConsultationRequest.ContextCareerDiscovery => await ResolveAnalysisAsync(studentId, contextRefId),
                ConsultationRequest.ContextPathwayPlan => await ResolvePlanAsync(studentId, contextRefId),
                ConsultationRequest.ContextRealityCheck => await ResolveReviewAsync(studentId, contextRefId),
                _ => new ContextSnapshot(ConsultationRequest.ContextGeneral, null, "General question", "{}", "{}")
            };
        }
        catch (KeyNotFoundException)
        {
            // Ownership failure or unknown id: fall back to a general request rather than revealing
            // whether the referenced record exists. The question is still answerable by a human.
            return new ContextSnapshot(
                ConsultationRequest.ContextGeneral,
                null,
                "General question (the referenced record was not available to this account)",
                "{}",
                "{}");
        }
    }

    private async Task<ContextSnapshot> ResolveAnalysisAsync(Guid studentId, Guid? analysisId)
    {
        if (analysisId == null)
        {
            throw new KeyNotFoundException("A pathway analysis id is required for this context.");
        }

        var analysis = await _context.PathwayAnalyses.AsNoTracking()
            .Include(a => a.StudentProfile)
            .SingleOrDefaultAsync(a => a.Id == analysisId && a.StudentProfile!.UserId == studentId)
            ?? throw new KeyNotFoundException("Pathway analysis not found for this student.");

        List<string> topCareers;
        try
        {
            using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(analysis.RecommendationsJson) ? "[]" : analysis.RecommendationsJson);
            topCareers = document.RootElement.EnumerateArray()
                .Take(5)
                .Select(item => item.TryGetProperty("pathway_name", out var name) && name.ValueKind == JsonValueKind.String
                    ? name.GetString() ?? string.Empty
                    : item.TryGetProperty("pathwayName", out var camel) && camel.ValueKind == JsonValueKind.String
                        ? camel.GetString() ?? string.Empty
                        : string.Empty)
                .Where(name => name.Length > 0)
                .ToList();
        }
        catch (JsonException)
        {
            topCareers = new List<string>();
        }

        var payload = new
        {
            pathwayAnalysisId = analysis.Id,
            status = analysis.Status,
            topCareers,
            workflowId = analysis.WorkflowId,
            analysedAt = analysis.CreatedAt
        };

        var summary = topCareers.Count > 0
            ? $"Career Discovery result · top matches: {string.Join(", ", topCareers)}"
            : "Career Discovery result";

        var evidence = await BuildStudentEvidenceAsync(studentId, analysisId, null);
        return new ContextSnapshot(
            ConsultationRequest.ContextCareerDiscovery,
            analysis.Id,
            summary,
            JsonSerializer.Serialize(payload, JsonOptions),
            evidence);
    }

    private async Task<ContextSnapshot> ResolvePlanAsync(Guid studentId, Guid? planId)
    {
        if (planId == null)
        {
            throw new KeyNotFoundException("A pathway plan id is required for this context.");
        }

        var plan = await _context.PathwayPlans.AsNoTracking()
            .Include(p => p.StudentProfile)
            .SingleOrDefaultAsync(p => p.Id == planId && p.StudentProfile!.UserId == studentId)
            ?? throw new KeyNotFoundException("Pathway plan not found for this student.");

        var completed = new List<string>();
        try
        {
            completed = JsonSerializer.Deserialize<List<string>>(
                string.IsNullOrWhiteSpace(plan.CompletedPhasesJson) ? "[]" : plan.CompletedPhasesJson) ?? new List<string>();
        }
        catch (JsonException)
        {
            completed = new List<string>();
        }

        var payload = new
        {
            pathwayPlanId = plan.Id,
            selectedPathway = plan.SelectedPathway,
            nextAction = plan.NextAction,
            completedStages = completed,
            workflowId = plan.WorkflowId
        };

        var summary = $"Roadmap: {plan.SelectedPathway} · {completed.Count} milestone(s) completed";
        var evidence = await BuildStudentEvidenceAsync(studentId, plan.Id, null);
        return new ContextSnapshot(
            ConsultationRequest.ContextPathwayPlan,
            plan.Id,
            summary,
            JsonSerializer.Serialize(payload, JsonOptions),
            evidence);
    }

    private async Task<ContextSnapshot> ResolveReviewAsync(Guid studentId, Guid? reviewId)
    {
        if (reviewId == null)
        {
            throw new KeyNotFoundException("A review id is required for this context.");
        }

        var review = await _context.PathwayReviews.AsNoTracking()
            .SingleOrDefaultAsync(r => r.Id == reviewId && r.StudentId == studentId)
            ?? throw new KeyNotFoundException("Review not found for this student.");

        var payload = new
        {
            reviewId = review.Id,
            targetCareer = review.TargetCareer,
            status = review.Status,
            feasibilityScore = review.FeasibilityScore,
            isHighRisk = review.IsHighRisk,
            riskReason = review.RiskReason,
            missingSkills = SafeList(review.MissingSkillsJson),
            counsellorFeedback = review.CounsellorFeedback
        };

        var summary = $"Reality Check: {review.TargetCareer} · {review.Status} · feasibility {review.FeasibilityScore}%";

        // For a Reality Check question the evidence is the review itself plus the pathway profile.
        var evidence = JsonSerializer.Serialize(new
        {
            review = payload,
            degreeRequirement = review.DegreeRequirement,
            costGuidance = review.CostGuidance,
            entryRequirements = SafeList(review.EntryRequirementsJson),
            validationResults = SafeList(review.ValidationResultsJson)
        }, JsonOptions);

        return new ContextSnapshot(
            ConsultationRequest.ContextRealityCheck,
            review.Id,
            summary,
            JsonSerializer.Serialize(payload, JsonOptions),
            evidence);
    }

    /// <summary>
    /// Compact, explicitly-scoped bundle of the student's own agent evidence for Agent 5.
    /// Contains no contact details and no other student's data.
    /// </summary>
    private async Task<string> BuildStudentEvidenceAsync(Guid studentId, Guid? analysisId, Guid? reviewId)
    {
        var profile = await _context.StudentProfiles.AsNoTracking()
            .SingleOrDefaultAsync(p => p.UserId == studentId);

        var latestPlan = await _context.PathwayPlans.AsNoTracking()
            .Where(p => p.StudentProfile!.UserId == studentId)
            .OrderByDescending(p => p.UpdatedAt)
            .FirstOrDefaultAsync();

        var latestReview = reviewId != null
            ? await _context.PathwayReviews.AsNoTracking().SingleOrDefaultAsync(r => r.Id == reviewId)
            : await _context.PathwayReviews.AsNoTracking()
                .Where(r => r.StudentId == studentId)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefaultAsync();

        var evidence = new
        {
            profile = profile == null ? null : new
            {
                academicStage = profile.AcademicStage,
                coreSkills = profile.CoreSkills,
                hobbiesInterests = profile.HobbiesInterests,
                careerAmbitions = profile.CareerAmbitions,
                alStream = profile.AlStream,
                alResults = profile.AlResults,
                budgetLevel = profile.BudgetLevel
            },
            latestRoadmap = latestPlan == null ? null : new
            {
                selectedPathway = latestPlan.SelectedPathway,
                nextAction = latestPlan.NextAction
            },
            latestReview = latestReview == null ? null : new
            {
                targetCareer = latestReview.TargetCareer,
                status = latestReview.Status,
                feasibilityScore = latestReview.FeasibilityScore,
                isHighRisk = latestReview.IsHighRisk,
                missingSkills = SafeList(latestReview.MissingSkillsJson)
            },
            askedFromAnalysisId = analysisId
        };

        var json = JsonSerializer.Serialize(evidence, JsonOptions);
        // Keep the AI payload bounded; the brief only needs the essentials.
        return json.Length > 6000 ? json.Substring(0, 6000) : json;
    }

    private static List<string> SafeList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
