using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Consultant;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

/// <summary>
/// Admin-only management of Consultant accounts and consultation metrics.
///
/// Provisioning lives here rather than in <c>AuthController</c> because public registration is now
/// Student-only: creating staff must require an authenticated Admin, not a public endpoint.
/// </summary>
[ApiController]
[Authorize(Roles = "Admin")]
[Route("api/admin/consultants")]
public class AdminConsultantController : ControllerBase
{
    private readonly IConsultantAccountService _accounts;
    private readonly IConsultantQueueService _queue;

    public AdminConsultantController(IConsultantAccountService accounts, IConsultantQueueService queue)
    {
        _accounts = accounts;
        _queue = queue;
    }

    private Guid CurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(value, out var id)
            ? id
            : throw new UnauthorizedAccessException("Valid user ID claim is required.");
    }

    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _accounts.GetAllAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsultantDto dto)
    {
        try
        {
            return Ok(await _accounts.CreateAsync(dto));
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> Update(Guid userId, [FromBody] UpdateConsultantDto dto)
    {
        var result = await _accounts.UpdateAsync(userId, dto);
        return result == null ? NotFound(new { message = "Consultant account not found." }) : Ok(result);
    }

    /// <summary>Volume, SLA compliance, satisfaction and AI-draft usefulness for the consultation channel.</summary>
    [HttpGet("/api/admin/consultation-metrics")]
    public async Task<IActionResult> GetMetrics() => Ok(await _accounts.GetMetricsAsync());

    /// <summary>Assigns an existing case to a consultant (used when a P1 case breaches its SLA).</summary>
    [HttpPost("cases/{consultationId:guid}/reassign/{consultantUserId:guid}")]
    public async Task<IActionResult> Reassign(Guid consultationId, Guid consultantUserId)
    {
        try
        {
            var result = await _queue.ReassignAsync(CurrentUserId(), consultationId, consultantUserId);
            return result == null ? NotFound(new { message = "Consultation not found." }) : Ok(result);
        }
        catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
    }
}
