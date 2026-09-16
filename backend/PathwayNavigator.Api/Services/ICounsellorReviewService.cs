using PathwayNavigator.Api.DTOs.Review;

namespace PathwayNavigator.Api.Services;

public interface ICounsellorReviewService
{
    Task<IEnumerable<PathwayReviewResponseDto>> GetPendingReviewsAsync();
    Task<PathwayReviewResponseDto?> GetReviewByIdAsync(Guid id);
    Task<PathwayReviewResponseDto?> SubmitDecisionAsync(Guid reviewId, Guid counsellorId, CounsellorDecisionDto dto);
    Task<PathwayReviewResponseDto?> GetStudentReviewStatusAsync(Guid studentId);
}