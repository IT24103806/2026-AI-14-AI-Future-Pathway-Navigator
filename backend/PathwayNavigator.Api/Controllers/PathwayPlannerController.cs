using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/pathway-planner")]
    public class PathwayPlannerController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IStudentProfileService _profileService;
        private readonly IPathwayPlanService _planService;

        public PathwayPlannerController(
            IAgentService agentService,
            IStudentProfileService profileService,
            IPathwayPlanService planService)
        {
            _agentService = agentService;
            _profileService = profileService;
            _planService = planService;
        }

        private Guid GetCurrentUserId()
        {
            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (Guid.TryParse(subClaim, out var userId))
            {
                return userId;
            }
            throw new UnauthorizedAccessException("User identifier missing from JWT claims.");
        }

        /// <summary>
        /// Builds (or refreshes) an Agent 3 roadmap for the selected pathway and persists it in PostgreSQL.
        /// If the student already has completed phases for this pathway and none were sent in the request,
        /// their saved progress is preserved.
        /// </summary>
        [HttpPost("plan")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayPlannerResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Plan([FromBody] PathwayPlannerRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.SelectedPathway))
            {
                return BadRequest(new { message = "Select a pathway before building a roadmap." });
            }

            var userId = GetCurrentUserId();
            var profile = await _profileService.GetProfileByUserIdAsync(userId);
            if (profile == null || !profile.IsOnboardingCompleted)
            {
                return BadRequest(new { message = "Please complete onboarding before building a roadmap." });
            }

            // Preserve previously saved progress for this pathway when no explicit phases were passed
            if (request.CompletedPhases == null || request.CompletedPhases.Count == 0)
            {
                var existingPlan = await _planService.GetByPathwayForUserAsync(userId, request.SelectedPathway);
                if (existingPlan?.CompletedPhases?.Count > 0)
                {
                    request.CompletedPhases = existingPlan.CompletedPhases;
                }
            }

            request.Profile = new PlannerProfileDto
            {
                AcademicStage = profile.AcademicStage,
                CoreSkills = profile.CoreSkills,
                CareerAmbitions = profile.CareerAmbitions
            };

            var result = await _agentService.ProcessAgent3PlanAsync(request, userId.ToString());
            if (result == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Failed to communicate with the Pathway Planner agent." });
            }

            if (string.Equals(result.Status, "ready", StringComparison.OrdinalIgnoreCase))
            {
                result = await _planService.SaveOrUpdateAsync(profile.Id, result, request.CompletedPhases);
            }

            return Ok(result);
        }

        /// <summary>
        /// Fetches the most recently updated Agent 3 roadmap owned by the current user.
        /// </summary>
        [HttpGet("me/latest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayPlannerResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatest()
        {
            var userId = GetCurrentUserId();
            var latest = await _planService.GetLatestForUserAsync(userId);

            if (latest == null)
            {
                return NotFound(new { message = "No saved pathway roadmap found for the current student." });
            }

            return Ok(latest);
        }

        /// <summary>
        /// Fetches all saved Agent 3 roadmaps owned by the current user, ordered by most recently updated.
        /// </summary>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyList<PathwayPlannerResponseDto>))]
        public async Task<IActionResult> GetMyPlans()
        {
            var userId = GetCurrentUserId();
            var plans = await _planService.GetAllForUserAsync(userId);
            return Ok(plans);
        }

        /// <summary>
        /// Updates the completed milestone phases for a persisted Agent 3 roadmap owned by the current user.
        /// </summary>
        [HttpPatch("{id:guid}/progress")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayPlannerResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProgress(Guid id, [FromBody] UpdateRoadmapProgressDto request)
        {
            var userId = GetCurrentUserId();
            var updated = await _planService.UpdateProgressAsync(id, userId, request?.CompletedPhases ?? new List<string>());

            if (updated == null)
            {
                return NotFound(new { message = $"Pathway roadmap '{id}' was not found." });
            }

            return Ok(updated);
        }
    }
}
