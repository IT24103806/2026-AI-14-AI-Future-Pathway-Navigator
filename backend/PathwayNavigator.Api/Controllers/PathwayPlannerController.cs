using System;
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

        public PathwayPlannerController(IAgentService agentService, IStudentProfileService profileService)
        {
            _agentService = agentService;
            _profileService = profileService;
        }

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

            var subClaim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (!Guid.TryParse(subClaim, out var userId))
            {
                return Unauthorized();
            }

            var profile = await _profileService.GetProfileByUserIdAsync(userId);
            if (profile == null || !profile.IsOnboardingCompleted)
            {
                return BadRequest(new { message = "Please complete onboarding before building a roadmap." });
            }

            request.Profile = new PlannerProfileDto
            {
                AcademicStage = profile.AcademicStage,
                CoreSkills = profile.CoreSkills,
                CareerAmbitions = profile.CareerAmbitions
            };

            var result = await _agentService.ProcessAgent3PlanAsync(request, userId.ToString());
            return result == null
                ? StatusCode(StatusCodes.Status502BadGateway, new { message = "Failed to communicate with the Pathway Planner agent." })
                : Ok(result);
        }
    }
}