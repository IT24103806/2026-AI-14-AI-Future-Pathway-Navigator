using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using PathwayNavigator.Api.Controllers;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests
{
    public class CareerDiscoveryControllerTests
    {
        private readonly Mock<IAgentService> _agentService = new();
        private readonly Mock<IStudentProfileService> _profileService = new();
        private readonly Mock<IPathwayAnalysisService> _analysisService = new();
        private readonly Guid _userId = Guid.NewGuid();

        private CareerDiscoveryController BuildController()
        {
            var controller = new CareerDiscoveryController(_agentService.Object, _profileService.Object, _analysisService.Object);

            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, _userId.ToString()) }, "Test");
            controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
            };
            return controller;
        }

        private static StudentProfileDto CompletedProfile(Guid userId) => new()
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            AcademicStage = "Undergraduate",
            CoreSkills = new List<string> { "python" },
            HobbiesInterests = new List<string> { "ai" },
            CareerAmbitions = "AI Engineer",
            IsOnboardingCompleted = true
        };

        [Fact]
        public async Task Analyze_ReturnsBadRequest_WhenOnboardingNotCompleted()
        {
            _profileService.Setup(s => s.GetProfileByUserIdAsync(_userId))
                .ReturnsAsync((StudentProfileDto?)null);

            var result = await BuildController().Analyze();

            Assert.IsType<BadRequestObjectResult>(result);
            _agentService.Verify(s => s.ProcessAgent2AnalysisAsync(It.IsAny<StudentProfileDto>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task Analyze_ReturnsBadGateway_WhenAgentServiceFails()
        {
            var profile = CompletedProfile(_userId);
            _profileService.Setup(s => s.GetProfileByUserIdAsync(_userId)).ReturnsAsync(profile);
            _agentService.Setup(s => s.ProcessAgent2AnalysisAsync(profile, _userId.ToString()))
                .ReturnsAsync((PathwayAnalysisResponseDto?)null);

            var result = await BuildController().Analyze();

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(StatusCodes.Status502BadGateway, statusResult.StatusCode);
        }

        [Fact]
        public async Task Analyze_ReturnsOk_AndPersistsResult_WhenSuccessful()
        {
            var profile = CompletedProfile(_userId);
            var agentResult = new PathwayAnalysisResponseDto { WorkflowId = "wf-1", Status = "pending_approval" };
            var persisted = new PathwayAnalysisResponseDto { Id = Guid.NewGuid(), WorkflowId = "wf-1", Status = "pending_approval" };

            _profileService.Setup(s => s.GetProfileByUserIdAsync(_userId)).ReturnsAsync(profile);
            _agentService.Setup(s => s.ProcessAgent2AnalysisAsync(profile, _userId.ToString())).ReturnsAsync(agentResult);
            _analysisService.Setup(s => s.CreateAsync(profile.Id, agentResult)).ReturnsAsync(persisted);

            var result = await BuildController().Analyze();

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(persisted, okResult.Value);
        }

        [Fact]
        public async Task GetById_ReturnsNotFound_WhenAnalysisMissing()
        {
            var id = Guid.NewGuid();
            _analysisService.Setup(s => s.GetByIdForUserAsync(id, _userId)).ReturnsAsync((PathwayAnalysisResponseDto?)null);

            var result = await BuildController().GetById(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }

        [Fact]
        public async Task GetById_ReturnsOk_WhenAnalysisFound()
        {
            var id = Guid.NewGuid();
            var dto = new PathwayAnalysisResponseDto { Id = id, Status = "approved" };
            _analysisService.Setup(s => s.GetByIdForUserAsync(id, _userId)).ReturnsAsync(dto);

            var result = await BuildController().GetById(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(dto, okResult.Value);
        }

        [Fact]
        public async Task Approve_ReturnsConflict_WhenAnalysisAlreadyDecided()
        {
            var id = Guid.NewGuid();
            _analysisService.Setup(s => s.SetStatusAsync(id, _userId, "approved"))
                .ThrowsAsync(new InvalidOperationException("already decided"));

            var result = await BuildController().Approve(id);

            Assert.IsType<ConflictObjectResult>(result);
        }

        [Fact]
        public async Task Approve_ReturnsOk_WhenSuccessful()
        {
            var id = Guid.NewGuid();
            var dto = new PathwayAnalysisResponseDto { Id = id, Status = "approved" };
            _analysisService.Setup(s => s.SetStatusAsync(id, _userId, "approved")).ReturnsAsync(dto);

            var result = await BuildController().Approve(id);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Same(dto, okResult.Value);
        }

        [Fact]
        public async Task Reject_ReturnsNotFound_WhenAnalysisMissing()
        {
            var id = Guid.NewGuid();
            _analysisService.Setup(s => s.SetStatusAsync(id, _userId, "rejected")).ReturnsAsync((PathwayAnalysisResponseDto?)null);

            var result = await BuildController().Reject(id);

            Assert.IsType<NotFoundObjectResult>(result);
        }
    }
}
