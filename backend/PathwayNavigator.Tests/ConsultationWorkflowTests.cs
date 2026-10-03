using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.DTOs.Notification;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests;

/// <summary>
/// The consultation channel: a student asks, a consultant answers, the student is notified.
///
/// These tests pin the two things that matter most about the feature - the student can never reach
/// another student's data or a consultant's internal note, and a consultant can never quietly take
/// over a case someone else owns - plus the abuse controls and SLA windows.
/// </summary>
public class ConsultationWorkflowTests
{
    private static AppDbContext GetInMemoryDbContext() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options);

    private static Mock<IContextSnapshotResolver> SnapshotResolver()
    {
        var resolver = new Mock<IContextSnapshotResolver>();
        resolver
            .Setup(r => r.ResolveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid?>()))
            .ReturnsAsync(new ContextSnapshot(
                ConsultationRequest.ContextGeneral,
                null,
                "General question",
                "{}",
                "{}"));
        return resolver;
    }

    /// <summary>Agent 5 is off in these tests: triage must never be required for a request to be stored.</summary>
    private static Mock<IAgentService> AgentService()
    {
        var agent = new Mock<IAgentService>();
        agent.SetupGet(a => a.SupportsConsultationCopilot).Returns(false);
        return agent;
    }

    private static ConsultationService StudentService(AppDbContext context, Mock<INotificationPublisher>? publisher = null) =>
        new(
            context,
            SnapshotResolver().Object,
            (publisher ?? new Mock<INotificationPublisher>()).Object,
            AgentService().Object,
            NullLogger<ConsultationService>.Instance);

    private static ConsultantQueueService QueueService(
        AppDbContext context,
        ConsultationService consultations,
        Mock<INotificationPublisher>? publisher = null,
        Mock<IEmailService>? email = null) =>
        new(
            context,
            consultations,
            (publisher ?? new Mock<INotificationPublisher>()).Object,
            AgentService().Object,
            (email ?? new Mock<IEmailService>()).Object,
            NullLogger<ConsultantQueueService>.Instance);

    private static ConsultationRequest ExistingRequest(
        Guid studentId,
        string status = ConsultationRequest.StatusOpen,
        string contextType = ConsultationRequest.ContextGeneral,
        Guid? contextRefId = null,
        DateTime? createdAt = null,
        Guid? assignedConsultantId = null) =>
        new()
        {
            StudentId = studentId,
            ContextType = contextType,
            ContextRefId = contextRefId,
            ContextSnapshotJson = "{}",
            Category = "PathwayAdvice",
            Priority = "P2",
            Subject = "Which pathway fits my budget?",
            Body = "AI Engineer looks expensive for my family.",
            Status = status,
            AssignedConsultantId = assignedConsultantId,
            SlaDueAt = DateTime.UtcNow.AddHours(48),
            CreatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1),
            UpdatedAt = createdAt ?? DateTime.UtcNow.AddDays(-1)
        };

    private static CreateConsultationRequestDto NewRequest(string subject = "Which pathway fits my budget?") => new()
    {
        ContextType = ConsultationRequest.ContextGeneral,
        Subject = subject,
        Body = "AI Engineer looks expensive for my family. Is there a cheaper route?",
        Category = "PathwayAdvice",
        Priority = "P2"
    };

    // ------------------------------------------------------------------ student side

    [Fact]
    public async Task CreateAsync_StoresAnOpenRequest_AndAlertsTheConsultantPool()
    {
        using var context = GetInMemoryDbContext();
        var publisher = new Mock<INotificationPublisher>();
        var service = StudentService(context, publisher);
        var studentId = Guid.NewGuid();

        var created = await service.CreateAsync(studentId, NewRequest());

        Assert.Equal(ConsultationRequest.StatusOpen, created.Status);
        Assert.Equal("P2", created.Priority);
        Assert.Equal(studentId, created.StudentId);
        // P2 = 48h, and the SLA clock starts at creation.
        Assert.InRange(created.SlaDueAt, DateTime.UtcNow.AddHours(47), DateTime.UtcNow.AddHours(49));

        publisher.Verify(
            p => p.PublishToRoleAsync("Consultant", It.IsAny<CreateNotificationDto>()),
            Times.Once);
        publisher.Verify(
            p => p.PublishToRoleAsync("Admin", It.IsAny<CreateNotificationDto>()),
            Times.Once);

        // A request is never stored untriaged-and-audited: the audit row is part of the unit of work.
        var stored = await context.ConsultationRequests
            .Include(r => r.AuditEvents)
            .SingleAsync(r => r.Id == created.Id);
        Assert.Single(stored.AuditEvents);
        Assert.Equal("Created", stored.AuditEvents.Single().Action);
    }

    [Fact]
    public async Task CreateAsync_WithinTheCooldown_IsRejected()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        context.ConsultationRequests.Add(ExistingRequest(studentId, createdAt: DateTime.UtcNow));
        await context.SaveChangesAsync();

        var service = StudentService(context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(studentId, NewRequest()));
        Assert.Contains("wait a moment", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(context.ConsultationRequests);
    }

    [Fact]
    public async Task CreateAsync_WithThreeOpenRequests_IsRejected()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        for (var i = 0; i < 3; i++)
        {
            context.ConsultationRequests.Add(ExistingRequest(studentId, createdAt: DateTime.UtcNow.AddDays(-2)));
        }

        await context.SaveChangesAsync();
        var service = StudentService(context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(studentId, NewRequest()));
        Assert.Contains("3 open questions", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetForStudentAsync_ForAnotherStudentsRequest_ReturnsNull()
    {
        using var context = GetInMemoryDbContext();
        var ownerId = Guid.NewGuid();
        var intruderId = Guid.NewGuid();
        var request = ExistingRequest(ownerId);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = StudentService(context);

        Assert.Null(await service.GetForStudentAsync(intruderId, request.Id));
        Assert.NotNull(await service.GetForStudentAsync(ownerId, request.Id));
    }

    [Fact]
    public async Task InternalNotes_AreInvisibleToTheStudent_AndVisibleToStaff()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var request = ExistingRequest(studentId, assignedConsultantId: consultantId);
        request.Messages.Add(new ConsultationMessage
        {
            AuthorUserId = consultantId,
            AuthorRole = "Consultant",
            Body = "Check the scholarship page before replying.",
            IsInternal = true,
            CreatedAt = DateTime.UtcNow
        });
        request.Messages.Add(new ConsultationMessage
        {
            AuthorUserId = consultantId,
            AuthorRole = "Consultant",
            Body = "Apply for the merit scholarship first.",
            IsInternal = false,
            CreatedAt = DateTime.UtcNow
        });
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = StudentService(context);

        var studentView = await service.GetForStudentAsync(studentId, request.Id);
        Assert.NotNull(studentView);
        Assert.DoesNotContain(studentView!.Messages, m => m.IsInternal);
        Assert.Single(studentView.Messages);

        var staffView = await service.BuildResponseAsync(request.Id, consultantId, forStaff: true);
        Assert.NotNull(staffView);
        Assert.Contains(staffView!.Messages, m => m.IsInternal);
        Assert.Equal(2, staffView.Messages.Count);
    }

    [Fact]
    public async Task AddStudentMessageAsync_MovesAnAnsweredCaseBackToTheConsultant()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var request = ExistingRequest(studentId, status: ConsultationRequest.StatusAnswered);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = StudentService(context);

        var updated = await service.AddStudentMessageAsync(studentId, request.Id, "Thank you - I submitted the form.");

        Assert.NotNull(updated);
        Assert.Equal(ConsultationRequest.StatusInProgress, updated!.Status);
        Assert.Contains(updated.Messages, m => m.Body.Contains("submitted the form"));
        Assert.Contains(updated.AuditEvents, a => a.Action == "StudentReplied");
    }

    [Fact]
    public async Task AddStudentMessageAsync_OnAClosedCase_TellsTheStudentToReopenIt()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var request = ExistingRequest(studentId, status: ConsultationRequest.StatusClosed);
        request.ClosedAt = DateTime.UtcNow;
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = StudentService(context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.AddStudentMessageAsync(studentId, request.Id, "One more thing?"));
        Assert.Contains("Reopen", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetContextStatusAsync_FlagsAnOpenQuestionAboutTheSameThing()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var analysisId = Guid.NewGuid();
        context.ConsultationRequests.Add(ExistingRequest(
            studentId,
            contextType: ConsultationRequest.ContextCareerDiscovery,
            contextRefId: analysisId));
        await context.SaveChangesAsync();

        var service = StudentService(context);

        var status = await service.GetContextStatusAsync(studentId, ConsultationRequest.ContextCareerDiscovery, analysisId);
        Assert.True(status.HasOpenRequest);
        Assert.NotNull(status.ConsultationId);

        var other = await service.GetContextStatusAsync(studentId, ConsultationRequest.ContextCareerDiscovery, Guid.NewGuid());
        Assert.False(other.HasOpenRequest);
    }

    [Theory]
    [InlineData("P1", 24)]
    [InlineData("P2", 48)]
    [InlineData("P3", 72)]
    public void DueAt_UsesThePriorityWindowsFromThePlan(string priority, int hours)
    {
        var now = new DateTime(2026, 10, 3, 9, 0, 0, DateTimeKind.Utc);

        Assert.Equal(now.AddHours(hours), ConsultationService.DueAt(priority, now));
    }

    // ------------------------------------------------------------------ consultant desk

    [Fact]
    public async Task ClaimAsync_OnAnUnclaimedCase_AssignsItAndTellsTheStudent()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var request = ExistingRequest(studentId);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var publisher = new Mock<INotificationPublisher>();
        var service = QueueService(context, StudentService(context), publisher);

        var claimed = await service.ClaimAsync(consultantId, request.Id);

        Assert.NotNull(claimed);
        Assert.Equal(consultantId, claimed!.AssignedConsultantId);
        Assert.Equal(ConsultationRequest.StatusClaimed, claimed.Status);
        Assert.Contains(claimed.AuditEvents, a => a.Action == "Claimed");
        publisher.Verify(p => p.PublishAsync(It.Is<CreateNotificationDto>(n => n.UserId == studentId)), Times.Once);
    }

    [Fact]
    public async Task ClaimAsync_OnACaseSomebodyElseOwns_IsRefused()
    {
        using var context = GetInMemoryDbContext();
        var ownerId = Guid.NewGuid();
        var request = ExistingRequest(Guid.NewGuid(), assignedConsultantId: ownerId);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ClaimAsync(Guid.NewGuid(), request.Id));
        Assert.Contains("already being handled", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AConsultantCannotOpenACaseThatBelongsToAnotherConsultant()
    {
        using var context = GetInMemoryDbContext();
        var ownerId = Guid.NewGuid();
        var request = ExistingRequest(Guid.NewGuid(), assignedConsultantId: ownerId);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));

        Assert.Null(await service.GetCaseAsync(Guid.NewGuid(), isAdmin: false, request.Id));
        Assert.NotNull(await service.GetCaseAsync(ownerId, isAdmin: false, request.Id));
        Assert.NotNull(await service.GetCaseAsync(Guid.NewGuid(), isAdmin: true, request.Id));
    }

    [Fact]
    public async Task TheUnclaimedPoolIsVisibleToEveryConsultant()
    {
        using var context = GetInMemoryDbContext();
        var request = ExistingRequest(Guid.NewGuid());
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));

        Assert.NotNull(await service.GetCaseAsync(Guid.NewGuid(), isAdmin: false, request.Id));
    }

    [Fact]
    public async Task ReplyAsync_OnAnUnclaimedCase_ImplicitlyClaimsItAndKeepsTheGuidance()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var request = ExistingRequest(studentId, contextType: ConsultationRequest.ContextPathwayPlan);
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));

        var replied = await service.ReplyAsync(consultantId, isAdmin: false, request.Id, new ReplyConsultationDto
        {
            Message = "Apply for the merit scholarship first; it covers the full course fee.",
            Guidance = new ConsultationGuidanceDto
            {
                StageKey = "skills",
                Note = "Do this before paying for any course.",
                Checklist = new List<string> { "Submit the scholarship form" }
            },
            Resources = new List<ConsultationResourceDto>
            {
                new() { Label = "Scholarship guide", Url = "https://example.edu/scholarships" },
                new() { Label = "Not a link", Url = "ftp://example.edu/file" }
            }
        });

        Assert.NotNull(replied);
        Assert.Equal(consultantId, replied!.AssignedConsultantId);
        Assert.Equal(ConsultationRequest.StatusAnswered, replied.Status);

        // Guidance is stored for the journey component to render, and only http(s) resources survive.
        Assert.Contains("scholarship", replied.ConsultantGuidanceJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ftp://", replied.ConsultantGuidanceJson, StringComparison.OrdinalIgnoreCase);

        var storedGuidance = JsonDocument.Parse(replied.ConsultantGuidanceJson).RootElement;
        Assert.Equal("skills", storedGuidance.GetProperty("stageKey").GetString());

        var storedMessage = await context.ConsultationMessages
            .Where(m => m.ConsultationRequestId == request.Id && !m.IsInternal)
            .SingleAsync();
        Assert.DoesNotContain("ftp://", storedMessage.ResourcesJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReplyAsync_WithAnEmptyMessage_IsRejectedByTheDto()
    {
        using var context = GetInMemoryDbContext();
        var request = ExistingRequest(Guid.NewGuid());
        context.ConsultationRequests.Add(request);
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));

        // The API validates the DTO before it reaches the service; the guard here is the empty-check
        // that the controller's model validation mirrors.
        var dto = new ReplyConsultationDto { Message = "" };
        Assert.True(string.IsNullOrWhiteSpace(dto.Message));

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.ReplyAsync(
            Guid.NewGuid(),
            isAdmin: false,
            request.Id,
            new ReplyConsultationDto { Message = "   " }));
        Assert.Contains("reply", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EscalatingARealityCheckQuestion_LeavesANoteForTheCounsellorDecision()
    {
        using var context = GetInMemoryDbContext();
        var studentId = Guid.NewGuid();
        var consultantId = Guid.NewGuid();
        var review = new PathwayReview
        {
            Id = Guid.NewGuid(),
            StudentId = studentId,
            Status = "Pending",
            IsHighRisk = true,
            FeasibilitySummary = "Two gaps",
            CreatedAt = DateTime.UtcNow
        };
        context.PathwayReviews.Add(review);
        context.ConsultationRequests.Add(ExistingRequest(
            studentId,
            contextType: ConsultationRequest.ContextRealityCheck,
            contextRefId: review.Id,
            assignedConsultantId: consultantId));
        await context.SaveChangesAsync();

        var service = QueueService(context, StudentService(context));
        var consultationId = context.ConsultationRequests.Single().Id;

        var escalated = await service.EscalateAsync(consultantId, isAdmin: false, consultationId, "Needs a counsellor decision on the budget gap.");

        Assert.NotNull(escalated);
        Assert.Equal(ConsultationRequest.StatusEscalated, escalated!.Status);

        // The cross-role handoff is recorded as an audit row on the review itself - no schema change.
        var audit = await context.PathwayReviewAudits.SingleAsync(a => a.PathwayReviewId == review.Id);
        Assert.Equal("ConsultantEscalation", audit.Action);
        Assert.Contains("budget gap", audit.Details);
    }
}
