using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OnboardingController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IStudentProfileService _profileService;

        public OnboardingController(IAgentService agentService, IStudentProfileService profileService)
        {
            _agentService = agentService;
            _profileService = profileService;
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
        /// Proxies conversational turns to Agent 1 (Python microservice) and auto-saves profile on completion.
        /// </summary>
        [HttpPost("chat")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(AgentChatResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ChatWithAgent1([FromBody] AgentChatRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            var response = await _agentService.ProcessAgent1TurnAsync(request, userId.ToString());

            if (response == null)
            {
                return StatusCode(500, new { message = "Failed to communicate with conversational AI agent." });
            }

            // Auto-persist profile if Agent 1 flags all slots complete
            if (response.IsComplete)
            {
                var saved = await _profileService.SaveFromExtractedSlotsAsync(userId, response.ExtractedSlots, "ConversationalAgent");
                response.IsSaved = saved;
            }

            return Ok(response);
        }

        /// <summary>
        /// Saves onboarding profile directly via standard form submission.
        /// </summary>
        [HttpPost("standard-form")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CompleteViaStandardForm([FromBody] CompleteOnboardingDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();
            request.OnboardingMethod = "StandardForm";

            var profile = await _profileService.SaveOrUpdateProfileAsync(userId, request);
            return Ok(profile);
        }
    }
}
