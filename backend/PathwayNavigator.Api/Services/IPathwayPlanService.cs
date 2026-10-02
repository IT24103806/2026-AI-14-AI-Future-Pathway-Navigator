using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Pathway;

namespace PathwayNavigator.Api.Services
{
    public interface IPathwayPlanService
    {
        Task<PathwayPlannerResponseDto> SaveOrUpdateAsync(
            Guid studentProfileId,
            PathwayPlannerResponseDto agentResult,
            List<string>? completedPhases = null);

        Task<PathwayPlannerResponseDto?> GetLatestForUserAsync(Guid userId);

        Task<PathwayPlannerResponseDto?> GetByPathwayForUserAsync(Guid userId, string selectedPathway);

        Task<IReadOnlyList<PathwayPlannerResponseDto>> GetAllForUserAsync(Guid userId);

        Task<PathwayPlannerResponseDto?> UpdateProgressAsync(Guid planId, Guid userId, List<string> completedPhases);
    }
}
