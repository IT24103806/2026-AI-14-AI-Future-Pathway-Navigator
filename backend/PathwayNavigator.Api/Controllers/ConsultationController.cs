using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

/// <summary>
/// Student-facing consultation endpoints.
///
/// The student identifier is always derived from the signed JWT - no route or body field can select
/// another student's data. Single-record lookups return 404 (not 403) for "not yours", so the endpoints
/// cannot be used to probe which ids exist.
/// </summary>
[ApiController]
[Authorize]
[Route("api/consultations")]
public class ConsultationController : ControllerBase
{
    private readonly IConsultationService _consultations;

    public ConsultationController(IConsultationService consultations) => _consultations = consultations;

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Valid user ID claim is required.");
    }

    /// <summary>Raises a question anchored to the pathway, roadmap stage or Reality Check the student is on.</summary>
    [HttpPost]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Create([FromBody] CreateConsultationRequestDto dto)
    {
        try
        {
            return Ok(await _consultations.CreateAsync(CurrentUserId(), dto));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpGet("me")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetMine(
        [FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            return Ok(await _consultations.GetMineAsync(CurrentUserId(), status, page, pageSize));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _consultations.GetForStudentAsync(CurrentUserId(), id);
        return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
    }

    [HttpPost("{id:guid}/messages")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> AddMessage(Guid id, [FromBody] ConsultationMessageInputDto dto)
    {
        try
        {
            var result = await _consultations.AddStudentMessageAsync(CurrentUserId(), id, dto.Body);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/close")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Close(Guid id, [FromBody] CloseConsultationDto dto)
    {
        try
        {
            var result = await _consultations.CloseAsync(CurrentUserId(), id, dto);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("{id:guid}/reopen")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> Reopen(Guid id)
    {
        try
        {
            var result = await _consultations.ReopenAsync(CurrentUserId(), id);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>
    /// Used by the journey components to show "you already asked about this" instead of a second form.
    /// </summary>
    [HttpGet("context/{contextType}/{refId:guid}")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> GetContextStatus(string contextType, Guid refId) =>
        Ok(await _consultations.GetContextStatusAsync(CurrentUserId(), contextType, refId));

    /// <summary>
    /// Pre-submit FAQ search. Always returns 200 with an empty list when the copilot is unavailable:
    /// the "ask a human" path must never be blocked by the AI path.
    /// </summary>
    [HttpGet("faq-match")]
    [Authorize(Roles = "Student")]
    public async Task<IActionResult> MatchFaq([FromQuery] string question)
    {
        if (string.IsNullOrWhiteSpace(question) || question.Trim().Length < 6)
        {
            return Ok(new AgentFaqMatchResultDto());
        }

        return Ok(await _consultations.MatchFaqAsync(question.Trim()) ?? new AgentFaqMatchResultDto());
    }
}
