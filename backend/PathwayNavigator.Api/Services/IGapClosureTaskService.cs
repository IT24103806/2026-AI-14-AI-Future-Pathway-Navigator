using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

public interface IGapClosureTaskService
{
    Task<GapClosureTask?> CreateAsync(Guid studentId, CreateGapClosureTaskDto request);
    Task<IReadOnlyList<GapClosureTask>> ListAsync(Guid studentId, Guid reviewId);
    Task<GapClosureTask?> UpdateAsync(Guid studentId, Guid id, GapClosureTaskDto request);
    Task<bool> DeleteAsync(Guid studentId, Guid id);
}
