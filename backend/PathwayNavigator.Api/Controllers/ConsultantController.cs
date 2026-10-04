using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

/// <summary>
/// The Consultant Desk API.
///
/// Scope note (least privilege): a Consultant can work the unclaimed pool and their own caseload only.
/// They deliberately <b>cannot</b> reach the counsellor approval queue or approve/reject a Reality Check -
/// guidance and authority are separate functions (see ADR-004 and the integration plan).
/// </summary>
[ApiController]
[Authorize(Roles = "Consultant,Admin")]
[Route("api/consultant")]
public class ConsultantController : ControllerBase
{
    private readonly IConsultantQueueService _queue;
    private readonly IConsultationService _consultations;

    public ConsultantController(IConsultantQueueService queue, IConsultationService consultations)
    {
        _queue = queue;
        _consultations = consultations;
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Valid user ID claim is required.");
    }

    private bool IsAdmin => User.IsInRole("Admin");

    [HttpGet("queue")]
    public async Task<IActionResult> GetQueue(
        [FromQuery] string scope = "Open",
        [FromQuery] string? category = null,
        [FromQuery] string? priority = null,
        [FromQuery] string? contextType = null,
        [FromQuery] string? search = null,
        [FromQuery] string sort = "sla",
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            return Ok(await _queue.GetQueueAsync(CurrentUserId(), IsAdmin, new ConsultantQueueFilter
            {
                Scope = scope,
                Category = category,
                Priority = priority,
                ContextType = contextType,
                Search = search,
                Sort = sort,
                Page = page,
                PageSize = pageSize
            }));
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpGet("queue/{id:guid}")]
    public async Task<IActionResult> GetCase(Guid id)
    {
        var result = await _queue.GetCaseAsync(CurrentUserId(), IsAdmin, id);
        return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
    }

    [HttpPost("queue/{id:guid}/claim")]
    public async Task<IActionResult> Claim(Guid id)
    {
        try
        {
            var result = await _queue.ClaimAsync(CurrentUserId(), id);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("queue/{id:guid}/release")]
    public async Task<IActionResult> Release(Guid id, [FromBody] ReleaseConsultationDto? dto)
    {
        var result = await _queue.ReleaseAsync(CurrentUserId(), IsAdmin, id, dto?.Reason);
        return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
    }

    [HttpPost("queue/{id:guid}/reply")]
    public async Task<IActionResult> Reply(Guid id, [FromBody] ReplyConsultationDto dto)
    {
        try
        {
            var result = await _queue.ReplyAsync(CurrentUserId(), IsAdmin, id, dto);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPost("queue/{id:guid}/note")]
    public async Task<IActionResult> AddNote(Guid id, [FromBody] InternalNoteDto dto)
    {
        var result = await _queue.AddInternalNoteAsync(CurrentUserId(), IsAdmin, id, dto.Body);
        return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
    }

    [HttpPost("queue/{id:guid}/priority")]
    public async Task<IActionResult> UpdatePriority(Guid id, [FromBody] UpdatePriorityDto dto)
    {
        var result = await _queue.UpdatePriorityAsync(CurrentUserId(), IsAdmin, id, dto.Priority);
        return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
    }

    [HttpPost("queue/{id:guid}/escalate")]
    public async Task<IActionResult> Escalate(Guid id, [FromBody] EscalateConsultationDto? dto)
    {
        try
        {
            var result = await _queue.EscalateAsync(CurrentUserId(), IsAdmin, id, dto?.Reason);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    /// <summary>Agent 5 case brief. Null when the copilot is unavailable - the desk works without it.</summary>
    [HttpGet("queue/{id:guid}/brief")]
    public async Task<IActionResult> GetBrief(Guid id) =>
        Ok(await _queue.GetBriefAsync(CurrentUserId(), IsAdmin, id));

    /// <summary>Agent 5 draft reply. The consultant edits it; nothing is ever sent automatically.</summary>
    [HttpPost("queue/{id:guid}/draft")]
    public async Task<IActionResult> GetDraft(Guid id, [FromBody] DraftRequestDto? dto) =>
        Ok(await _queue.GetDraftAsync(CurrentUserId(), IsAdmin, id, DraftRequestDto.ParseTone(dto?.Tone)));

    [HttpGet("me/stats")]
    public async Task<IActionResult> GetStats() => Ok(await _queue.GetStatsAsync(CurrentUserId(), IsAdmin));

    [HttpGet("me/profile")]
    public async Task<IActionResult> GetProfile()
    {
        var result = await _queue.GetProfileAsync(CurrentUserId());
        return result == null ? NotFound(new { message = "Consultant profile not found." }) : Ok(result);
    }

    [HttpPut("me/profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] DTOs.Consultant.UpdateConsultantSelfDto dto)
    {
        var result = await _queue.UpdateSelfAsync(CurrentUserId(), dto);
        return result == null ? NotFound(new { message = "Consultant profile not found." }) : Ok(result);
    }

    /// <summary>Student lookup used only to render the thread - the queue payload already scopes the data.</summary>
    [HttpGet("students/{consultationId:guid}/context")]
    public async Task<IActionResult> GetStudentContext(Guid consultationId)
    {
        var result = await _queue.GetCaseAsync(CurrentUserId(), IsAdmin, consultationId);
        return result == null
            ? NotFound(new { message = "Consultation not found." })
            : Ok(new { result.StudentId, result.StudentName, result.ContextType, result.ContextSummary, result.ContextSnapshotJson });
    }
}

public class ReleaseConsultationDto
{
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Reason { get; set; }
}

public class EscalateConsultationDto
{
    [System.ComponentModel.DataAnnotations.StringLength(500)]
    public string? Reason { get; set; }
}

public class DraftRequestDto
{
    /// <summary>
    /// "Supportive" | "Direct" | "Detailed". Sent as a string by both clients, and the API has no
    /// global string-enum converter, so it is parsed here instead of bound as an enum.
    /// An unknown tone falls back to Supportive rather than failing the request.
    /// </summary>
    public string? Tone { get; set; }

    public static DraftTone ParseTone(string? tone) => tone?.Trim().ToLowerInvariant() switch
    {
        "direct" => DraftTone.Direct,
        "detailed" => DraftTone.Detailed,
        _ => DraftTone.Supportive,
    };
}
