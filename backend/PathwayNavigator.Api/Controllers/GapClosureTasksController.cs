using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Services;

namespace PathwayNavigator.Api.Controllers;

[ApiController, Authorize(Roles = "Student")]
[Route("api/gap-closure-tasks")]
public class GapClosureTasksController : ControllerBase
{
    private readonly IGapClosureTaskService _tasks;
    public GapClosureTasksController(IGapClosureTaskService tasks) => _tasks = tasks;
    private Guid StudentId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub")!);

    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid reviewId) => reviewId == Guid.Empty
        ? BadRequest(new { message = "reviewId is required." })
        : Ok(await _tasks.ListAsync(StudentId(), reviewId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateGapClosureTaskDto request)
    {
        try
        {
            var task = await _tasks.CreateAsync(StudentId(), request);
            return task == null ? NotFound(new { message = "Review not found for this student." }) : StatusCode(201, task);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] GapClosureTaskDto request)
    {
        try
        {
            var task = await _tasks.UpdateAsync(StudentId(), id, request);
            return task == null ? NotFound() : Ok(task);
        }
        catch (ArgumentException ex) { return BadRequest(new { message = ex.Message }); }
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) => await _tasks.DeleteAsync(StudentId(), id) ? NoContent() : NotFound();
}
