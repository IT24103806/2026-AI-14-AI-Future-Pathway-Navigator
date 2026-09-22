using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CounsellorReviewController : ControllerBase
{
    private readonly ICounsellorReviewService _reviewService;

    public CounsellorReviewController(ICounsellorReviewService reviewService)
    {
        _reviewService = reviewService;
    }

    // Endpoint 1: Counsellor ට pending reviews බැලීම
    [HttpGet("pending")]
    [Authorize(Roles = "Counsellor,Admin")]
    public async Task<IActionResult> GetPendingReviews()
    {
        var reviews = await _reviewService.GetPendingReviewsAsync();
        return Ok(reviews);
    }

    // Endpoint 2: විශේෂිත review එකක විස්තර බැලීම
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetReviewById(Guid id)
    {
        var review = await _reviewService.GetReviewByIdAsync(id);
        if (review == null) return NotFound("Review record not found");
        return Ok(review);
    }

    // Endpoint 3: Counsellor Decision Submit කිරීම (Core Business Logic)
    [HttpPost("{id}/decision")]
    [Authorize(Roles = "Counsellor,Admin")]
    public async Task<IActionResult> SubmitDecision(Guid id, [FromBody] CounsellorDecisionDto dto)
    {
        var counsellorIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Guid counsellorId = counsellorIdClaim != null ? Guid.Parse(counsellorIdClaim) : Guid.Empty;

        var updated = await _reviewService.SubmitDecisionAsync(id, counsellorId, dto);
        if (updated == null) return NotFound("Review not found");

        return Ok(updated);
    }

    // Endpoint 4: Mobile App එකෙන් ශිෂ්‍යයාට තමන්ගේ review status බැලීම
    [HttpGet("student/{studentId}/status")]
    [Authorize]
    public async Task<IActionResult> GetStudentStatus(Guid studentId)
    {
        var status = await _reviewService.GetStudentReviewStatusAsync(studentId);
        if (status == null) return NotFound("No reviews found for this student");
        return Ok(status);
    }
}