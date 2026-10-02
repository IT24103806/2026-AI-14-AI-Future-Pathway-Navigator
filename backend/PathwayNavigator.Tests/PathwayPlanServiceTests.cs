using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PathwayNavigator.Api.Controllers;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests
{
    public class PathwayPlanServiceTests
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

        private static PathwayPlannerResponseDto SamplePlanResult(string pathway = "AI / Machine Learning Engineer") => new()
        {
            WorkflowId = "wf-plan-1",
            Status = "ready",
            SelectedPathway = pathway,
            MissingSkills = new List<string> { "deep learning", "sql" },
            NextAction = "Compare accredited degree options.",
            Roadmap = new List<RoadmapStageDto>
            {
                new()
                {
                    Order = 1,
                    Stage = "education",
                    Title = "Build the education foundation",
                    Outcome = "Reach education milestone",
                    Actions = new List<string> { "Compare accredited degree options." },
                    EstimatedDuration = "3-4 years",
                    Status = "not_started"
                },
                new()
                {
                    Order = 2,
                    Stage = "skills",
                    Title = "Close the core skill gaps",
                    Outcome = "Reach skills milestone",
                    Actions = new List<string> { "Study Machine Learning Specialization." },
                    EstimatedDuration = "3-6 months",
                    Status = "not_started"
                },
                new()
                {
                    Order = 3,
                    Stage = "certification",
                    Title = "Add evidence of readiness",
                    Outcome = "Reach certification milestone",
                    Actions = new List<string> { "Complete Deep Learning with PyTorch." },
                    EstimatedDuration = "3-6 months",
                    Status = "not_started"
                }
            }
        };

        [Fact]
        public async Task SaveOrUpdateAsync_CreatesAndUpsertsPlanForSamePathway()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var service = new PathwayPlanService(context);

            var created = await service.SaveOrUpdateAsync(profileId, SamplePlanResult(), new List<string> { "education" });

            Assert.NotNull(created.Id);
            Assert.Single(created.CompletedPhases);
            Assert.Equal("education", created.CompletedPhases[0]);
            Assert.Equal("completed", created.Roadmap[0].Status);
            Assert.Equal("not_started", created.Roadmap[1].Status);
            Assert.Equal("Study Machine Learning Specialization.", created.NextAction);

            // Upserting the same pathway updates the existing row rather than duplicating it
            var updated = await service.SaveOrUpdateAsync(profileId, SamplePlanResult(), new List<string> { "education", "skills" });

            Assert.Equal(created.Id, updated.Id);
            Assert.Equal(2, updated.CompletedPhases.Count);
            Assert.Equal("Complete Deep Learning with PyTorch.", updated.NextAction);
            var allPlans = await service.GetAllForUserAsync(userId);
            Assert.Single(allPlans);
        }

        [Fact]
        public async Task UpdateProgressAsync_UpdatesCompletedStages_AndAdvancesNextAction()
        {
            await using var context = NewContext();
            var (userId, profileId) = SeedUserWithProfile(context);
            var service = new PathwayPlanService(context);
            var saved = await service.SaveOrUpdateAsync(profileId, SamplePlanResult());

            var afterFirst = await service.UpdateProgressAsync(saved.Id!.Value, userId, new List<string> { "education" });
            Assert.NotNull(afterFirst);
            Assert.Equal(new List<string> { "education" }, afterFirst!.CompletedPhases);
            Assert.Equal("Study Machine Learning Specialization.", afterFirst.NextAction);

            // Complete all stages
            var afterAll = await service.UpdateProgressAsync(
                saved.Id!.Value,
                userId,
                new List<string> { "education", "skills", "certification" });
            Assert.NotNull(afterAll);
            Assert.Equal(3, afterAll!.CompletedPhases.Count);
            Assert.Contains("All roadmap milestones", afterAll.NextAction);

            // Another user cannot update this student's plan
            var unauthorized = await service.UpdateProgressAsync(saved.Id!.Value, Guid.NewGuid(), new List<string>());
            Assert.Null(unauthorized);
        }

        [Fact]
        public async Task PathwayPlannerController_PreservesSavedProgress_AndPersistsPlan()
        {
            var agentService = new Mock<IAgentService>();
            var profileService = new Mock<IStudentProfileService>();
            var planService = new Mock<IPathwayPlanService>();
            var userId = Guid.NewGuid();
            var profileId = Guid.NewGuid();

            var profile = new StudentProfileDto
            {
                Id = profileId,
                UserId = userId,
                AcademicStage = "Undergraduate",
                CoreSkills = new List<string> { "Python" },
                CareerAmbitions = "AI Engineer",
                IsOnboardingCompleted = true
            };

            var existingSaved = SamplePlanResult();
            existingSaved.Id = Guid.NewGuid();
            existingSaved.CompletedPhases = new List<string> { "education" };

            var agentResponse = SamplePlanResult();
            var persistedResponse = SamplePlanResult();
            persistedResponse.Id = existingSaved.Id;
            persistedResponse.CompletedPhases = new List<string> { "education" };

            profileService.Setup(s => s.GetProfileByUserIdAsync(userId)).ReturnsAsync(profile);
            planService.Setup(s => s.GetByPathwayForUserAsync(userId, "AI / Machine Learning Engineer")).ReturnsAsync(existingSaved);
            agentService.Setup(s => s.ProcessAgent3PlanAsync(
                    It.Is<PathwayPlannerRequestDto>(r => r.CompletedPhases.Contains("education")),
                    userId.ToString()))
                .ReturnsAsync(agentResponse);
            planService.Setup(s => s.SaveOrUpdateAsync(profileId, agentResponse, It.IsAny<List<string>>()))
                .ReturnsAsync(persistedResponse);

            var controller = new PathwayPlannerController(agentService.Object, profileService.Object, planService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) }, "Test"))
                    }
                }
            };

            var actionResult = await controller.Plan(new PathwayPlannerRequestDto
            {
                SelectedPathway = "AI / Machine Learning Engineer",
                CompletedPhases = new List<string>()
            });

            var okResult = Assert.IsType<OkObjectResult>(actionResult);
            Assert.Same(persistedResponse, okResult.Value);
        }
    }
}
