using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests;

public class GapClosureTaskServiceTests
{
    [Fact]
    public async Task Student_can_create_read_update_and_delete_only_own_task()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var owner = Guid.NewGuid(); var other = Guid.NewGuid(); var reviewId = Guid.NewGuid();
        db.PathwayReviews.Add(new PathwayReview { Id = reviewId, StudentId = owner, WorkflowId = "test-review", TargetCareer = "Engineer" });
        await db.SaveChangesAsync();
        var service = new GapClosureTaskService(db);
        Assert.Null(await service.CreateAsync(other, new CreateGapClosureTaskDto { PathwayReviewId = reviewId, Title = "Wrong owner" }));
        var created = await service.CreateAsync(owner, new CreateGapClosureTaskDto { PathwayReviewId = reviewId, Title = "Study Python" });
        Assert.NotNull(created);
        Assert.Empty(await service.ListAsync(other, reviewId));
        Assert.Single(await service.ListAsync(owner, reviewId));
        Assert.Null(await service.UpdateAsync(other, created!.Id, new GapClosureTaskDto { Title = "Tampered", Status = "Done" }));
        Assert.False(await service.DeleteAsync(other, created.Id));
        await service.UpdateAsync(owner, created.Id, new GapClosureTaskDto { Title = "Finish Python course", Status = "Done" });
        Assert.Equal("Done", (await service.ListAsync(owner, reviewId)).Single().Status);
        Assert.True(await service.DeleteAsync(owner, created.Id));
        Assert.Empty(await service.ListAsync(owner, reviewId));
    }

    [Fact]
    public async Task Invalid_status_is_rejected()
    {
        using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var service = new GapClosureTaskService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateAsync(Guid.NewGuid(),
            new CreateGapClosureTaskDto { PathwayReviewId = Guid.NewGuid(), Title = "Learn", Status = "Approved" }));
    }
}
