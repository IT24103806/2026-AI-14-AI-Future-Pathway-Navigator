using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Notification;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Background SLA clock for the consultation channel. Runs a few times an hour and:
///
/// 1. warns the assigned consultant (or the whole pool) when a case passes its SLA due time;
/// 2. escalates an <b>unclaimed P1</b> case to Admin after 12 hours, so a blocked student is never
///    waiting on an empty pool;
/// 3. expires cases the student abandoned (<c>AwaitingStudent</c> older than 14 days) and closes them
///    with a resolution note.
///
/// Every rule is idempotent: the notification carries a stable <c>DedupeKey</c>, so running the sweep
/// twice cannot double-notify, and a re-run after a restart cannot re-escalate the same case.
/// Failure is logged and swallowed - a broken sweep must never take the API down.
/// </summary>
public sealed class ConsultationSlaHostedService : BackgroundService
{
    private static readonly TimeSpan UnclaimedP1EscalationAfter = TimeSpan.FromHours(12);
    private static readonly TimeSpan AwaitingStudentExpiryAfter = TimeSpan.FromDays(14);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ConsultationSlaHostedService> _logger;
    private readonly TimeSpan _interval;
    private readonly bool _enabled;

    public ConsultationSlaHostedService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<ConsultationSlaHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _enabled = configuration.GetValue("Consultation:SlaSweepEnabled", true);
        var seconds = configuration.GetValue("Consultation:SlaSweepSeconds", 300);
        _interval = TimeSpan.FromSeconds(Math.Clamp(seconds, 60, 3600));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            using var timer = new PeriodicTimer(_interval);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await SweepAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notifications = scope.ServiceProvider.GetRequiredService<INotificationPublisher>();
            var now = DateTime.UtcNow;

            // 1 + 2 -----------------------------------------------------------------------------
            var active = await context.ConsultationRequests
                .Where(r => r.ClosedAt == null)
                .ToListAsync(cancellationToken);

            var adminRoleNotification = new CreateNotificationDto
            {
                UserId = Guid.Empty,
                Type = "ConsultationSlaBreach",
                Title = "A student question has breached its SLA and is unclaimed",
                Body = "Assign it to a consultant or answer it directly.",
                DeepLink = "/admin/dashboard",
                EntityType = "Consultation",
                Priority = "Action"
            };

            foreach (var request in active)
            {
                if (now <= request.SlaDueAt)
                {
                    continue;
                }

                var overdueHours = Math.Round((now - request.SlaDueAt).TotalHours, 1);

                if (request.AssignedConsultantId.HasValue)
                {
                    await SafePublishAsync(notifications, new CreateNotificationDto
                    {
                        UserId = request.AssignedConsultantId.Value,
                        Type = "ConsultationSlaBreach",
                        Title = $"SLA breached: {request.Subject}",
                        Body = $"{request.Priority} case is {overdueHours}h overdue.",
                        DeepLink = $"/consultant/dashboard?consultation={request.Id}",
                        EntityType = "Consultation",
                        EntityId = request.Id,
                        Priority = "Action",
                        // One warning per (case, day) - the case keeps its place in the queue but stops re-alerting.
                        DedupeKey = $"sla-breach:{request.Id}:{now:yyyyMMdd}"
                    });
                }
                else if (request.Priority == "P1" && now - request.CreatedAt >= UnclaimedP1EscalationAfter)
                {
                    var escalation = new CreateNotificationDto
                    {
                        UserId = Guid.Empty,
                        Type = "ConsultationSlaBreach",
                        Title = $"P1 unclaimed for over 12h: {request.Subject}",
                        Body = "No consultant has picked this up. Assign it or answer the student directly.",
                        DeepLink = "/admin/dashboard",
                        EntityType = "Consultation",
                        EntityId = request.Id,
                        Priority = "Action",
                        DedupeKey = $"sla-escalate-admin:{request.Id}"
                    };
                    await SafePublishToRoleAsync(notifications, "Admin", escalation);
                }
            }

            // 3 ---------------------------------------------------------------------------------
            var staleCutoff = now - AwaitingStudentExpiryAfter;
            var stale = await context.ConsultationRequests
                .Where(r => r.ClosedAt == null
                            && r.Status == ConsultationRequest.StatusAwaitingStudent
                            && r.UpdatedAt < staleCutoff)
                .ToListAsync(cancellationToken);

            foreach (var request in stale)
            {
                var previous = request.Status;
                request.Status = ConsultationRequest.StatusClosed;
                request.ClosedAt = now;
                request.UpdatedAt = now;
                request.ResolutionSummary ??= "Closed automatically after 14 days without a reply from the student.";
                request.AuditEvents.Add(new ConsultationAudit
                {
                    ActorUserId = null,
                    Action = "AutoExpired",
                    FromStatus = previous,
                    ToStatus = ConsultationRequest.StatusClosed,
                    Details = "No student response within 14 days.",
                    CreatedAt = now
                });

                await SafePublishAsync(notifications, new CreateNotificationDto
                {
                    UserId = request.StudentId,
                    Type = "ConsultationAutoExpired",
                    Title = "A question was closed automatically",
                    Body = $"{request.Subject} - you can reopen it for 7 days, or ask again.",
                    DeepLink = $"/student/support?consultation={request.Id}",
                    EntityType = "Consultation",
                    EntityId = request.Id,
                    Priority = "Warning",
                    DedupeKey = $"auto-expire:{request.Id}"
                });
            }

            if (active.Count > 0 || stale.Count > 0)
            {
                await context.SaveChangesAsync(cancellationToken);
            }

            _logger.LogDebug("Consultation SLA sweep completed: {Active} active, {Expired} auto-expired.", active.Count, stale.Count);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Consultation SLA sweep failed. The API keeps running and will retry on the next tick.");
        }
    }

    private async Task SafePublishAsync(INotificationPublisher publisher, CreateNotificationDto notification)
    {
        try
        {
            await publisher.PublishAsync(notification);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "SLA notification {Type} could not be published.", notification.Type);
        }
    }

    private async Task SafePublishToRoleAsync(INotificationPublisher publisher, string role, CreateNotificationDto notification)
    {
        try
        {
            await publisher.PublishToRoleAsync(role, notification);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "SLA role notification {Type} could not be published.", notification.Type);
        }
    }
}
