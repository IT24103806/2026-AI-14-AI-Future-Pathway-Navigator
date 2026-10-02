using PathwayNavigator.Api.DTOs.Review;

namespace PathwayNavigator.Api.Services;

public interface ICounsellorReviewService
{
    Task<IReadOnlyList<PathwayReviewResponseDto>> GetPendingReviewsAsync();
    Task<PagedReviewsDto> GetReviewsAsync(string status, string? search, string sort, int page, int pageSize);
    Task<PathwayReviewResponseDto?> GetReviewByIdAsync(Guid id);
    Task<PathwayReviewResponseDto?> GetReviewByIdForStudentAsync(Guid id, Guid studentId);
    Task<PathwayReviewResponseDto> CreateAsync(Guid analysisId, Guid studentId, StartRealityCheckDto input);
    Task<PathwayReviewResponseDto> ResubmitAsync(Guid reviewId, Guid studentId, StartRealityCheckDto input);
    Task<PathwayReviewResponseDto?> SubmitDecisionAsync(Guid reviewId, Guid counsellorId, CounsellorDecisionDto dto);
    Task<PathwayReviewResponseDto?> GetStudentReviewStatusAsync(Guid studentId);
    Task<IReadOnlyList<PathwayReviewResponseDto>> GetStudentHistoryAsync(Guid studentId);
}
