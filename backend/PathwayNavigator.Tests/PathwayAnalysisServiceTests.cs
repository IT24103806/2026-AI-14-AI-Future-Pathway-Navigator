using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests
{
    public class PathwayAnalysisServiceTests
    {
        private static AppDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static (Guid userId, Guid profileId) SeedUserWithProfile(AppDbContext context)
        {
            var user = new User { Id = Guid.NewGuid(), Email = $"{Guid.NewGuid()}@test.com", RoleId = Guid.NewGuid() };
            var profile = new StudentProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AcademicStage = "Undergraduate",
                CareerAmbitions = "AI Engineer",
                IsOnboardingCompleted = true
            };
            context.Users.Add(user);
            context.StudentProfiles.Add(profile);
            context.SaveChanges();
            return (user.Id, profile.Id);
        }

        private static PathwayAnalysisResponseDto SampleAgentResult() => new()
        {
            WorkflowId = Guid.NewGuid().ToString(),
            Status = "pending_approval",
            Recommendations = new()
            {
                new CareerPathRecommendationDto
                {
                    Label = "Path A",
                    PathwayName = "AI / Machine Learning Engineer",
                    MatchScore = 70,
                    DayInTheLife = "Builds and deploys ML pipelines.",
                    SalaryRangeLkr = "LKR 180,000 – 450,000/mo",
                    IndustryTools = new() { "PyTorch", "Docker" },
                    PortfolioProjects = new() { "RAG Document Q&A Assistant" },
                    RecommendedCertifications = new() { "DeepLearning.AI ML Specialization" },
                    SriLankanEducationRoutes = new() { "SLIIT BSc (Hons) in IT - AI" }
                }
            }
        };

        [Fact]
        public async Task CreateAsync_PersistsRecord_AndSetsId()
        {
            await using var context = NewContext();
            var (_, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            var agentResult = SampleAgentResult();

            var saved = await service.CreateAsync(profileId, agentResult);

            Assert.NotNull(saved.Id);
            Assert.NotEqual(Guid.Empty, saved.Id!.Value);
            var stored = await context.PathwayAnalyses.FindAsync(saved.Id!.Value);
            Assert.NotNull(stored);
            Assert.Equal("pending_approval", stored!.Status);
            Assert.Equal(profileId, stored.StudentProfileId);
        }

        [Fact]
        public async Task GetByIdForUserAsync_ReturnsNull_WhenNotOwnedByCaller()
        {
            await using var context = NewContext();
            var (_, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            var saved = await service.CreateAsync(profileId, SampleAgentResult());

            var result = await service.GetByIdForUserAsync(saved.Id!.Value, Guid.NewGuid());

            Assert.Null(result);
        }

        [Fact]
        public async Task GetByIdForUserAsync_ReturnsAnalysis_WhenOwnedByCaller()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            var saved = await service.CreateAsync(profileId, SampleAgentResult());

            var result = await service.GetByIdForUserAsync(saved.Id!.Value, userId);

            Assert.NotNull(result);
            Assert.Single(result!.Recommendations);
            Assert.Equal("AI / Machine Learning Engineer", result.Recommendations[0].PathwayName);
            Assert.Equal("LKR 180,000 – 450,000/mo", result.Recommendations[0].SalaryRangeLkr);
            Assert.Contains("PyTorch", result.Recommendations[0].IndustryTools);
            Assert.Contains("RAG Document Q&A Assistant", result.Recommendations[0].PortfolioProjects);
            Assert.Contains("DeepLearning.AI ML Specialization", result.Recommendations[0].RecommendedCertifications);
            Assert.Contains("SLIIT BSc (Hons) in IT - AI", result.Recommendations[0].SriLankanEducationRoutes);
        }

        [Fact]
        public async Task SetStatusAsync_ApprovesPendingAnalysis_AndStampsApprover()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            var saved = await service.CreateAsync(profileId, SampleAgentResult());

            var result = await service.SetStatusAsync(saved.Id!.Value, userId, "approved");

            Assert.NotNull(result);
            Assert.Equal("approved", result!.Status);
            var stored = await context.PathwayAnalyses.FindAsync(saved.Id!.Value);
            Assert.Equal("approved", stored!.Status);
            Assert.Equal(userId, stored.ApprovedByUserId);
            Assert.NotNull(stored.ApprovedAt);
        }

        [Fact]
        public async Task SetStatusAsync_Throws_WhenAnalysisAlreadyDecided()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            var saved = await service.CreateAsync(profileId, SampleAgentResult());
            await service.SetStatusAsync(saved.Id!.Value, userId, "approved");

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => service.SetStatusAsync(saved.Id!.Value, userId, "rejected"));
        }

        [Fact]
        public async Task SetStatusAsync_ReturnsNull_WhenAnalysisNotFoundForCaller()
        {
            await using var context = NewContext();
            var (_, profileId) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);
            await service.CreateAsync(profileId, SampleAgentResult());

            var result = await service.SetStatusAsync(Guid.NewGuid(), Guid.NewGuid(), "approved");

            Assert.Null(result);
        }

        [Fact]
        public async Task GetLatestForUserAsync_ReturnsMostRecentAnalysis_ForOwnerOnly()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var (otherUserId, _) = SeedUserWithProfile(context);
            var service = new PathwayAnalysisService(context);

            var older = await service.CreateAsync(profileId, new PathwayAnalysisResponseDto
            {
                WorkflowId = "wf-older",
                Status = "pending_approval",
                Recommendations = new() { new CareerPathRecommendationDto { PathwayName = "Data Scientist / Data Analyst" } }
            });
            var olderEntity = await context.PathwayAnalyses.FindAsync(older.Id!.Value);
            olderEntity!.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
            await context.SaveChangesAsync();

            var latest = await service.CreateAsync(profileId, new PathwayAnalysisResponseDto
            {
                WorkflowId = "wf-latest",
                Status = "approved",
                Recommendations = new() { new CareerPathRecommendationDto { PathwayName = "AI / Machine Learning Engineer" } }
            });

            var foundForOwner = await service.GetLatestForUserAsync(userId);
            var foundForOther = await service.GetLatestForUserAsync(otherUserId);
            var history = await service.GetHistoryForUserAsync(userId);

            Assert.NotNull(foundForOwner);
            Assert.Equal(latest.Id, foundForOwner!.Id);
            Assert.Equal("wf-latest", foundForOwner.WorkflowId);
            Assert.Null(foundForOther);
            Assert.Equal(2, history.Count);
            Assert.Equal("wf-latest", history[0].WorkflowId);
            Assert.Equal("wf-older", history[1].WorkflowId);
        }
    }
}
