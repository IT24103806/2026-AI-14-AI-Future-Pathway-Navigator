using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/counsellor-review")]
public class CounsellorReviewController : ControllerBase
{
    private readonly ICounsellorReviewService _reviewService;
    public CounsellorReviewController(ICounsellorReviewService reviewService) => _reviewService = reviewService;

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id) ? id : throw new UnauthorizedAccessException("Valid user ID claim is required.");
    }

    [HttpPost("analysis/{analysisId:guid}/evaluate")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> StartRealityCheck(Guid analysisId, [FromBody] StartRealityCheckDto dto)
    {
        try { return Ok(await _reviewService.CreateAsync(analysisId, CurrentUserId(), dto)); }
        catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        catch (HttpRequestException ex) { return StatusCode(502, new { message = ex.Message }); }
        catch (InvalidDataException ex) { return StatusCode(502, new { message = ex.Message }); }
    }

    [HttpGet]
    [Authorize(Roles = "Counsellor,Admin")]
    public async Task<IActionResult> GetReviews(
        [FromQuery] string status = "Pending", [FromQuery] string? search = null,
        [FromQuery] string sort = "newest", [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try { return Ok(await _reviewService.GetReviewsAsync(status, search, sort, page, pageSize)); }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        var isStaff = User.IsInRole("Counsellor") || User.IsInRole("Admin");
        var review = isStaff
            ? await _reviewService.GetReviewByIdAsync(id)
            : await _reviewService.GetReviewByIdForStudentAsync(id, CurrentUserId());
        return review == null ? NotFound(new { message = "Review record not found." }) : Ok(review);
    }

    [HttpPost("{id:guid}/decision")]
    [Authorize(Roles = "Counsellor,Admin")]
    public async Task<IActionResult> SubmitDecision(Guid id, [FromBody] CounsellorDecisionDto dto)
    {
        try
        {
            var updated = await _reviewService.SubmitDecisionAsync(id, CurrentUserId(), dto);
            return updated == null ? NotFound(new { message = "Review not found." }) : Ok(updated);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("me/status")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyStatus()
    {
        var status = await _reviewService.GetStudentReviewStatusAsync(CurrentUserId());
        return status == null ? NotFound(new { message = "No pathway review has been submitted." }) : Ok(status);
    }

    [HttpGet("me/history")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMyHistory() => Ok(await _reviewService.GetStudentHistoryAsync(CurrentUserId()));
}
