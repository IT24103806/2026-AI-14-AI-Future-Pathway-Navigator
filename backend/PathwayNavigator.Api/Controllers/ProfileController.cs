using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IStudentProfileService _profileService;

        public ProfileController(IStudentProfileService profileService)
        {
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
        /// Checks whether the currently authenticated student has completed the onboarding profile.
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ProfileStatusDto))]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetStatus()
        {
            var userId = GetCurrentUserId();
            var status = await _profileService.GetProfileStatusAsync(userId);
            return Ok(status);
        }

        /// <summary>
        /// Retrieves the student's profile information.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StudentProfileDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetProfile()
        {
            var userId = GetCurrentUserId();
            var profile = await _profileService.GetProfileByUserIdAsync(userId);

            if (profile == null)
            {
                return NotFound(new { message = "Student profile has not been created yet." });
            }

            return Ok(profile);
        }

        /// <summary>
        /// Updates the student's personal details (full name, academic stage, skills, interests,
        /// ambitions, A/L context). Only the fields present in the payload are changed, so the
        /// "Re-run AI onboarding" flow and the profile editor can send partial updates.
        /// </summary>
        [HttpPut]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(StudentProfileDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateStudentProfileDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var userId = GetCurrentUserId();

            try
            {
                var profile = await _profileService.UpdateProfileAsync(userId, request);
                return Ok(profile);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
        }
    }
}
