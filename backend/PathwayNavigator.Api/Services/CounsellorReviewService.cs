using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Review;

namespace PathwayNavigator.Api.Services;

public class CounsellorReviewService : ICounsellorReviewService
{
    private readonly AppDbContext _context;

    public CounsellorReviewService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<PathwayReviewResponseDto>> GetPendingReviewsAsync()
    {
        return await _context.PathwayReviews
            .Where(r => r.Status == "Pending")
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new PathwayReviewResponseDto
            {
                Id = r.Id,
                PathwayAnalysisId = r.PathwayAnalysisId,
                StudentId = r.StudentId,
                Status = r.Status,
                IsHighRisk = r.IsHighRisk,
                RiskReason = r.RiskReason,
                MissingSkillsJson = r.MissingSkillsJson,
                FeasibilitySummary = r.FeasibilitySummary,
                CreatedAt = r.CreatedAt
            }).ToListAsync();
    }

    public async Task<PathwayReviewResponseDto?> GetReviewByIdAsync(Guid id)
    {
        var review = await _context.PathwayReviews.FindAsync(id);
        if (review == null) return null;

        return new PathwayReviewResponseDto
        {
            Id = review.Id,
            PathwayAnalysisId = review.PathwayAnalysisId,
            StudentId = review.StudentId,
            Status = review.Status,
            IsHighRisk = review.IsHighRisk,
            RiskReason = review.RiskReason,
            MissingSkillsJson = review.MissingSkillsJson,
            FeasibilitySummary = review.FeasibilitySummary,
            CounsellorFeedback = review.CounsellorFeedback,
            CreatedAt = review.CreatedAt,
            ReviewedAt = review.ReviewedAt
        };
    }

    public async Task<PathwayReviewResponseDto?> SubmitDecisionAsync(Guid reviewId, Guid counsellorId, CounsellorDecisionDto dto)
    {
        var review = await _context.PathwayReviews.FindAsync(reviewId);
        if (review == null) return null;

        review.Status = dto.Decision; // Approved, Rejected, NeedsRevision
        review.CounsellorFeedback = dto.Feedback;
        review.CounsellorId = counsellorId;
        review.ReviewedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetReviewByIdAsync(reviewId);
    }

    public async Task<PathwayReviewResponseDto?> GetStudentReviewStatusAsync(Guid studentId)
    {
        var review = await _context.PathwayReviews
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        if (review == null) return null;

        return await GetReviewByIdAsync(review.Id);
    }
}