using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Review;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;
using Moq;

namespace PathwayNavigator.Tests;

public class CounsellorReviewServiceTests
{
    private readonly Mock<IAgentService> _agentService = new();
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetPendingReviewsAsync_ShouldReturnOnlyPendingRecords()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        context.PathwayReviews.AddRange(
            new PathwayReview
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Status = "Pending",
                IsHighRisk = true,
                FeasibilitySummary = "High Risk: Stream mismatch",
                CreatedAt = DateTime.UtcNow
            },
            new PathwayReview
            {
                Id = Guid.NewGuid(),
                StudentId = Guid.NewGuid(),
                Status = "Approved",
                IsHighRisk = false,
                FeasibilitySummary = "Direct Feasible Match",
                CreatedAt = DateTime.UtcNow
            }
        );
        await context.SaveChangesAsync();

        var service = new CounsellorReviewService(context, _agentService.Object);

        // Act
        var result = await service.GetPendingReviewsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("Pending", result.First().Status);
    }

    [Theory]
    [InlineData("Approved")]
    [InlineData("Rejected")]
    [InlineData("NeedsRevision")]
    public async Task SubmitDecisionAsync_ShouldUpdateReviewStatusAndFeedback(string decision)
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var reviewId = Guid.NewGuid();
        var counsellorId = Guid.NewGuid();

        var review = new PathwayReview
        {
            Id = reviewId,
            StudentId = Guid.NewGuid(),
            Status = "Pending",
            IsHighRisk = true,
            FeasibilitySummary = "Pending Counsellor Decision",
            CreatedAt = DateTime.UtcNow
        };
        context.PathwayReviews.Add(review);
        await context.SaveChangesAsync();

        var service = new CounsellorReviewService(context, _agentService.Object);
        var decisionDto = new CounsellorDecisionDto
        {
            Decision = decision,
            Feedback = "Reviewed by Senior Counsellor. Follow remedial steps."
        };

        // Act
        var result = await service.SubmitDecisionAsync(reviewId, counsellorId, decisionDto);
        var updatedReview = await context.PathwayReviews.FindAsync(reviewId);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(decision, result.Status);
        Assert.NotNull(updatedReview);
        Assert.Equal(decision, updatedReview.Status);
        Assert.Equal(decisionDto.Feedback, updatedReview.CounsellorFeedback);
        Assert.Equal(counsellorId, updatedReview.CounsellorId);
        Assert.NotNull(updatedReview.ReviewedAt);
    }

    [Fact]
    public async Task SubmitDecisionAsync_ShouldRejectSecondDecision()
    {
        using var context = GetInMemoryDbContext();
        var review = new PathwayReview
        {
            StudentId = Guid.NewGuid(), PathwayAnalysisId = Guid.NewGuid(), WorkflowId = "wf-test",
            TargetCareer = "AI Engineer", Status = "Approved", CounsellorFeedback = "Already completed"
        };
        context.PathwayReviews.Add(review);
        await context.SaveChangesAsync();
        var service = new CounsellorReviewService(context, _agentService.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitDecisionAsync(
            review.Id, Guid.NewGuid(), new CounsellorDecisionDto { Decision = "Rejected", Feedback = "Try changing it." }));
    }

    [Fact]
    public async Task GetReviewByIdForStudentAsync_ShouldEnforceOwnership()
    {
        using var context = GetInMemoryDbContext();
        var ownerId = Guid.NewGuid();
        var review = new PathwayReview
        {
            StudentId = ownerId, PathwayAnalysisId = Guid.NewGuid(), WorkflowId = "wf-owner",
            TargetCareer = "Software Engineer", Status = "Pending"
        };
        context.PathwayReviews.Add(review);
        await context.SaveChangesAsync();
        var service = new CounsellorReviewService(context, _agentService.Object);

        Assert.NotNull(await service.GetReviewByIdForStudentAsync(review.Id, ownerId));
        Assert.Null(await service.GetReviewByIdForStudentAsync(review.Id, Guid.NewGuid()));
    }
}
