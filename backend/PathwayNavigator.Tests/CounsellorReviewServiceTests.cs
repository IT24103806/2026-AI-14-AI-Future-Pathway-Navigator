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

namespace PathwayNavigator.Tests;

public class CounsellorReviewServiceTests
{
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

        var service = new CounsellorReviewService(context);

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

        var service = new CounsellorReviewService(context);
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
}