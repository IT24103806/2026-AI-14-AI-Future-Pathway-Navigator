using System;
using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Pathway;

namespace PathwayNavigator.Api.Services
{
    public interface IPathwayAnalysisService
    {
        Task<PathwayAnalysisResponseDto> CreateAsync(Guid studentProfileId, PathwayAnalysisResponseDto agentResult);

        Task<PathwayAnalysisResponseDto?> GetByIdForUserAsync(Guid id, Guid userId);

        /// <summary>
        /// Moves a pending_approval analysis to "approved" or "rejected".
        /// Returns null if not found/not owned by this user.
        /// Throws InvalidOperationException if the analysis isn't currently pending_approval.
        /// </summary>
        Task<PathwayAnalysisResponseDto?> SetStatusAsync(Guid id, Guid userId, string newStatus);
    }
}
