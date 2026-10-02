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
    /// <summary>
    /// Fulfils the "Python service must operate as an internal service called by
    /// ASP.NET Core, never directly by clients" backend rule: the React/Flutter
    /// clients only ever talk to this controller, which in turn calls the Python
    /// Agent 2 microservice via IAgentService and persists the human-approval
    /// decision (approve/reject) in PostgreSQL.
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/career-discovery")]
    public class CareerDiscoveryController : ControllerBase
    {
        private readonly IAgentService _agentService;
        private readonly IStudentProfileService _profileService;
        private readonly IPathwayAnalysisService _analysisService;

        public CareerDiscoveryController(
            IAgentService agentService,
            IStudentProfileService profileService,
            IPathwayAnalysisService analysisService)
        {
            _agentService = agentService;
            _profileService = profileService;
            _analysisService = analysisService;
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
        /// Runs Agent 2 (Career Discovery) against the current user's saved onboarding
        /// profile and persists the result as a pending_approval PathwayAnalysis row.
        /// </summary>
        [HttpPost("analyze")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayAnalysisResponseDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status502BadGateway)]
        public async Task<IActionResult> Analyze()
        {
            var userId = GetCurrentUserId();
            var profile = await _profileService.GetProfileByUserIdAsync(userId);

            if (profile == null || !profile.IsOnboardingCompleted)
            {
                return BadRequest(new { message = "Please complete onboarding (Agent 1) before running Career Discovery." });
            }

            var result = await _agentService.ProcessAgent2AnalysisAsync(profile, userId.ToString());

            if (result == null)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Failed to communicate with the Career Discovery agent." });
            }

            var saved = await _analysisService.CreateAsync(profile.Id, result);
            return Ok(saved);
        }

        /// <summary>
        /// Fetches the most recent pathway analysis owned by the current user.
        /// </summary>
        [HttpGet("me/latest")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayAnalysisResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatest()
        {
            var userId = GetCurrentUserId();
            var analysis = await _analysisService.GetLatestForUserAsync(userId);

            if (analysis == null)
            {
                return NotFound(new { message = "No saved pathway analysis found for the current student." });
            }

            return Ok(analysis);
        }

        /// <summary>
        /// Fetches all previously generated pathway analyses owned by the current user, newest first.
        /// </summary>
        [HttpGet("me/history")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(System.Collections.Generic.IReadOnlyList<PathwayAnalysisResponseDto>))]
        public async Task<IActionResult> GetHistory()
        {
            var userId = GetCurrentUserId();
            var history = await _analysisService.GetHistoryForUserAsync(userId);
            return Ok(history);
        }

        /// <summary>
        /// Fetches a previously generated pathway analysis owned by the current user.
        /// </summary>
        [HttpGet("{id:guid}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayAnalysisResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var userId = GetCurrentUserId();
            var analysis = await _analysisService.GetByIdForUserAsync(id, userId);

            if (analysis == null)
            {
                return NotFound(new { message = $"Pathway analysis '{id}' was not found." });
            }

            return Ok(analysis);
        }

        /// <summary>
        /// Human sign-off: approves a pending_approval analysis (e.g. the student/advisor
        /// confirms it should be written into the official profile).
        /// </summary>
        [HttpPatch("{id:guid}/approve")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayAnalysisResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Approve(Guid id)
        {
            return await ChangeStatus(id, "approved");
        }

        /// <summary>
        /// Human sign-off: rejects a pending_approval analysis.
        /// </summary>
        [HttpPatch("{id:guid}/reject")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PathwayAnalysisResponseDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> Reject(Guid id)
        {
            return await ChangeStatus(id, "rejected");
        }

        private async Task<IActionResult> ChangeStatus(Guid id, string newStatus)
        {
            var userId = GetCurrentUserId();
            try
            {
                var updated = await _analysisService.SetStatusAsync(id, userId, newStatus);
                if (updated == null)
                {
                    return NotFound(new { message = $"Pathway analysis '{id}' was not found." });
                }
                return Ok(updated);
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }
    }
}
