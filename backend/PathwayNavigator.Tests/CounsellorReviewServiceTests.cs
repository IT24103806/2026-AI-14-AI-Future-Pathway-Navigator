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

    [Fact]
    public async Task CreateAndResubmit_PersistsInputContext_SyncsProfile_AndAllowsResubmissionAfterNeedsRevision()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var counsellorId = Guid.NewGuid();
        var profile = new StudentProfile
        {
            Id = Guid.NewGuid(),
            UserId = studentId,
            AcademicStage = "After A/L",
            CoreSkills = new List<string> { "Python" },
            HobbiesInterests = new List<string> { "Coding" },
            CareerAmbitions = "Data Science"
        };
        var analysis = new PathwayAnalysis
        {
            Id = Guid.NewGuid(),
            StudentProfileId = profile.Id,
            StudentProfile = profile,
            RecommendationsJson = "[]"
        };
        context.StudentProfiles.Add(profile);
        context.PathwayAnalyses.Add(analysis);
        await context.SaveChangesAsync();

        var agentMock = new Mock<IAgentService>();
        agentMock
            .SetupSequence(a => a.ProcessAgent4RealityCheckAsync(It.IsAny<RealityCheckAgentRequestDto>()))
            .ReturnsAsync(new RealityCheckAgentResponseDto
            {
                WorkflowId = "wf-initial-1",
                Status = "pending_counsellor_review",
                CounsellorReviewRequired = true,
                IsHighRisk = true,
                RiskReason = "Missing Statistics and SQL fundamentals.",
                FeasibilityScore = 54,
                MissingSkills = new List<string> { "Statistics", "SQL" },
                SkillGapSummary = "Complete Statistics and SQL modules."
            })
            .ReturnsAsync(new RealityCheckAgentResponseDto
            {
                WorkflowId = "wf-resubmitted-2",
                Status = "approved",
                CounsellorReviewRequired = false,
                IsHighRisk = false,
                RiskReason = "Low risk pathway.",
                FeasibilityScore = 88,
                MissingSkills = new List<string>(),
                SkillGapSummary = "All core requirements satisfied."
            });

        var service = new CounsellorReviewService(context, agentMock.Object);

        // 1. Initial Reality Check evaluation
        var initialReview = await service.CreateAsync(analysis.Id, studentId, new StartRealityCheckDto
        {
            TargetCareer = "Data Science & AI",
            AlStream = "Physical Science",
            AlResults = "a, b, c",
            BudgetLevel = "Medium",
            CurrentSkills = new List<string> { "Python", "Git" }
        });

        Assert.Equal("Pending", initialReview.Status);
        Assert.Equal("Physical Science", initialReview.AlStream);
        Assert.Equal("A, B, C", initialReview.AlResults);
        Assert.Equal("Medium", initialReview.BudgetLevel);
        Assert.Contains("Git", initialReview.CurrentSkillsJson);

        // Verify StudentProfile was synced with A/L context and merged skills
        var updatedProfile = await context.StudentProfiles.SingleAsync(p => p.UserId == studentId);
        Assert.Equal("Physical Science", updatedProfile.AlStream);
        Assert.Equal("A, B, C", updatedProfile.AlResults);
        Assert.Equal("Medium", updatedProfile.BudgetLevel);
        Assert.Contains("Python", updatedProfile.CoreSkills);
        Assert.Contains("Git", updatedProfile.CoreSkills);

        // Cannot resubmit while still Pending
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ResubmitAsync(initialReview.Id, studentId, new StartRealityCheckDto
            {
                TargetCareer = "Data Science & AI",
                AlStream = "Physical Science",
                AlResults = "A, B, C",
                BudgetLevel = "Medium",
                CurrentSkills = new List<string> { "Python", "Git", "SQL" }
            }));

        // 2. Counsellor marks as NeedsRevision
        var revisedDecision = await service.SubmitDecisionAsync(initialReview.Id, counsellorId, new CounsellorDecisionDto
        {
            Decision = "NeedsRevision",
            Feedback = "Complete the foundational SQL & Statistics bridge course before re-applying."
        });
        Assert.NotNull(revisedDecision);
        Assert.Equal("NeedsRevision", revisedDecision!.Status);

        // 3. Student revises skills and resubmits
        var resubmittedReview = await service.ResubmitAsync(initialReview.Id, studentId, new StartRealityCheckDto
        {
            TargetCareer = "Data Science & AI",
            AlStream = "Physical Science",
            AlResults = "A, B, C",
            BudgetLevel = "Medium",
            CurrentSkills = new List<string> { "Python", "Git", "SQL", "Statistics" }
        });

        Assert.Equal("Approved", resubmittedReview.Status);
        Assert.Equal(88, resubmittedReview.FeasibilityScore);
        Assert.True(await context.PathwayReviewAudits.AnyAsync(e =>
            e.Action == "RealityCheckResubmitted" && e.FromStatus == "NeedsRevision"));

        var history = await service.GetStudentHistoryAsync(studentId);
        Assert.Equal(2, history.Count);
        Assert.Contains("Statistics", updatedProfile.CoreSkills);
    }
}
