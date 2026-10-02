using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

public class GapClosureTaskService : IGapClosureTaskService
{
    private readonly AppDbContext _db;
    public GapClosureTaskService(AppDbContext db) => _db = db;

    private static void Validate(GapClosureTaskDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 160)
            throw new ArgumentException("Task title must be between 1 and 160 characters.");
        if (request.Status is not ("ToDo" or "InProgress" or "Done"))
            throw new ArgumentException("Status must be ToDo, InProgress or Done.");
        if (request.DueDate?.Kind == DateTimeKind.Unspecified)
            throw new ArgumentException("Due date must include a timezone.");
    }

    public async Task<GapClosureTask?> CreateAsync(Guid studentId, CreateGapClosureTaskDto request)
    {
        Validate(request);
        if (!await _db.PathwayReviews.AnyAsync(r => r.Id == request.PathwayReviewId && r.StudentId == studentId)) return null;
        var task = new GapClosureTask { StudentId = studentId, PathwayReviewId = request.PathwayReviewId,
            Title = request.Title.Trim(), Status = request.Status, DueDate = request.DueDate?.ToUniversalTime() };
        _db.GapClosureTasks.Add(task);
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<IReadOnlyList<GapClosureTask>> ListAsync(Guid studentId, Guid reviewId) =>
        await _db.GapClosureTasks.AsNoTracking().Where(t => t.StudentId == studentId && t.PathwayReviewId == reviewId)
            .OrderBy(t => t.CreatedAt).ToListAsync();

    public async Task<GapClosureTask?> UpdateAsync(Guid studentId, Guid id, GapClosureTaskDto request)
    {
        Validate(request);
        var task = await _db.GapClosureTasks.SingleOrDefaultAsync(t => t.Id == id && t.StudentId == studentId);
        if (task == null) return null;
        task.Title = request.Title.Trim(); task.Status = request.Status;
        task.DueDate = request.DueDate?.ToUniversalTime(); task.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        return task;
    }

    public async Task<bool> DeleteAsync(Guid studentId, Guid id)
    {
        var task = await _db.GapClosureTasks.SingleOrDefaultAsync(t => t.Id == id && t.StudentId == studentId);
        if (task == null) return false;
        _db.GapClosureTasks.Remove(task);
        await _db.SaveChangesAsync();
        return true;
    }
}
