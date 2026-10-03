using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.DTOs.Consultant;
using PathwayNavigator.Api.DTOs.Notification;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// The Consultant Desk: pool triage, claiming, replies, escalation and the write-back that puts an
/// answer back inside the journey component the student asked from.
///
/// Authorization rules enforced here (not only in the controller):
/// * a consultant may act on unclaimed pool cases and on cases assigned to them;
/// * only Admin may act on/see another consultant's assigned case;
/// * a Consultant can never approve or reject a Reality Check - that stays with Counsellor/Admin (ADR-004).
/// </summary>
public class ConsultantQueueService : IConsultantQueueService
{
    private const int MaxGuidanceEntries = 20;

    private readonly AppDbContext _context;
    private readonly IConsultationService _consultations;
    private readonly INotificationPublisher _notifications;
    private readonly IAgentService _agentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<ConsultantQueueService> _logger;

    public ConsultantQueueService(
        AppDbContext context,
        IConsultationService consultations,
        INotificationPublisher notifications,
        IAgentService agentService,
        IEmailService emailService,
        ILogger<ConsultantQueueService> logger)
    {
        _context = context;
        _consultations = consultations;
        _notifications = notifications;
        _agentService = agentService;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<PagedConsultationsDto> GetQueueAsync(Guid consultantId, bool isAdmin, ConsultantQueueFilter filter)
    {
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 50);

        var query = _context.ConsultationRequests.AsNoTracking()
            .Include(r => r.Student)
            .Include(r => r.AssignedConsultant)
            .AsQueryable();

        var scope = (filter.Scope ?? "Open").Trim();
        if (string.Equals(scope, "Open", StringComparison.OrdinalIgnoreCase))
        {
            // The unclaimed pool: still actionable, no owner.
            query = query.Where(r => r.AssignedConsultantId == null && r.ClosedAt == null);
        }
        else if (string.Equals(scope, "Mine", StringComparison.OrdinalIgnoreCase))
        {
            query = query.Where(r => r.AssignedConsultantId == consultantId && r.ClosedAt == null);
        }
        else if (string.Equals(scope, "Unresolved", StringComparison.OrdinalIgnoreCase))
        {
            // Everything not yet closed that this consultant is allowed to see.
            query = isAdmin
                ? query.Where(r => r.ClosedAt == null)
                : query.Where(r => r.ClosedAt == null
                                   && (r.AssignedConsultantId == null || r.AssignedConsultantId == consultantId));
        }
        else if (!string.Equals(scope, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!ConsultationService.AllowedStatuses.Contains(scope))
            {
                throw new ArgumentException("Invalid queue scope or status.");
            }

            query = query.Where(r => r.Status == scope);
        }

        // A consultant only ever sees the pool plus their own caseload; Admin sees everything.
        if (!isAdmin)
        {
            query = query.Where(r => r.AssignedConsultantId == null || r.AssignedConsultantId == consultantId);
        }

        if (!string.IsNullOrWhiteSpace(filter.Category))
        {
            query = query.Where(r => r.Category == filter.Category);
        }

        if (!string.IsNullOrWhiteSpace(filter.Priority))
        {
            query = query.Where(r => r.Priority == filter.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.ContextType))
        {
            query = query.Where(r => r.ContextType == filter.ContextType);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(r => r.Subject.ToLower().Contains(term)
                                     || r.Body.ToLower().Contains(term)
                                     || r.Student!.Email.ToLower().Contains(term));
        }

        // "sla" is the default because the desk must work the most urgent case first.
        query = filter.Sort switch
        {
            "newest" => query.OrderByDescending(r => r.CreatedAt),
            "oldest" => query.OrderBy(r => r.CreatedAt),
            "priority" => query.OrderBy(r => r.Priority).ThenBy(r => r.SlaDueAt),
            _ => query.OrderBy(r => r.SlaDueAt)
        };

        var total = await query.CountAsync();
        var rows = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        var items = new List<ConsultationResponseDto>(rows.Count);
        foreach (var row in rows)
        {
            // List rows intentionally omit the thread: the desk loads one case at a time.
            items.Add(await _consultations.MapAsync(row, forStaff: true));
        }

        return new PagedConsultationsDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ConsultationResponseDto?> GetCaseAsync(Guid consultantId, bool isAdmin, Guid consultationId)
    {
        var request = await LoadCaseAsync(consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> ClaimAsync(Guid consultantId, Guid consultationId)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null)
        {
            return null;
        }

        if (request.AssignedConsultantId == consultantId)
        {
            return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
        }

        if (request.AssignedConsultantId != null)
        {
            var owner = await _context.Users.AsNoTracking()
                .Where(u => u.Id == request.AssignedConsultantId)
                .Select(u => u.FullName ?? u.Email)
                .FirstOrDefaultAsync();
            throw new InvalidOperationException($"This case is already being handled by {owner ?? "another consultant"}.");
        }

        if (request.ClosedAt != null)
        {
            throw new InvalidOperationException("This case is closed and cannot be claimed.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.AssignedConsultantId = consultantId;
        request.Status = ConsultationRequest.StatusClaimed;
        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "Claimed",
            FromStatus = previous,
            ToStatus = ConsultationRequest.StatusClaimed,
            Details = "A consultant picked up this case.",
            CreatedAt = now
        });

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // Two consultants pressed Claim at the same moment; the loser is told, not silently overwritten.
            throw new InvalidOperationException("Another consultant claimed this case a moment ago. Refresh the queue.");
        }

        await PublishSafeAsync(new CreateNotificationDto
        {
            UserId = request.StudentId,
            Type = "ConsultationClaimed",
            Title = "A consultant is looking into your question",
            Body = request.Subject,
            DeepLink = DeepLinkFor(request),
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Info"
        });

        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> ReleaseAsync(Guid consultantId, bool isAdmin, Guid consultationId, string? reason)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        if (request.AssignedConsultantId == null)
        {
            throw new InvalidOperationException("This case is not assigned to anyone.");
        }

        if (request.ClosedAt != null)
        {
            throw new InvalidOperationException("A closed case cannot be released.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.AssignedConsultantId = null;
        request.Status = ConsultationRequest.StatusOpen;
        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "Released",
            FromStatus = previous,
            ToStatus = ConsultationRequest.StatusOpen,
            Details = string.IsNullOrWhiteSpace(reason) ? "Returned to the pool." : $"Returned to the pool: {reason.Trim()}",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> ReassignAsync(Guid adminId, Guid consultationId, Guid consultantUserId)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null)
        {
            return null;
        }

        if (request.ClosedAt != null)
        {
            throw new InvalidOperationException("A closed case cannot be reassigned.");
        }

        var target = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .SingleOrDefaultAsync(u => u.Id == consultantUserId && u.IsActive);
        if (target == null || (target.Role?.Name != "Consultant" && target.Role?.Name != "Admin"))
        {
            throw new InvalidOperationException("The selected user is not an active consultant.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.AssignedConsultantId = consultantUserId;
        request.Status = ConsultationRequest.StatusClaimed;
        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = adminId,
            Action = "Reassigned",
            FromStatus = previous,
            ToStatus = ConsultationRequest.StatusClaimed,
            Details = $"Assigned to {target.FullName ?? target.Email}.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        await PublishSafeAsync(new CreateNotificationDto
        {
            UserId = consultantUserId,
            Type = "ConsultationAssigned",
            Title = "A case was assigned to you",
            Body = request.Subject,
            DeepLink = $"/consultant/dashboard?consultation={request.Id}",
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Action"
        });

        return await _consultations.BuildResponseAsync(consultationId, adminId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> ReplyAsync(Guid consultantId, bool isAdmin, Guid consultationId, ReplyConsultationDto input)
    {
        var request = await _context.ConsultationRequests
            .Include(r => r.Student)
            .SingleOrDefaultAsync(r => r.Id == consultationId);

        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        if (request.ClosedAt != null)
        {
            throw new InvalidOperationException("This case is closed. Ask the student to reopen it, or raise a new question.");
        }

        // Defence in depth: the DTO validates this too, but an empty reply must never reach a student
        // even if a future caller bypasses model validation.
        if (string.IsNullOrWhiteSpace(input.Message) || input.Message.Trim().Length < 2)
        {
            throw new InvalidOperationException("Write a reply before sending.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;

        // Replying to an unclaimed case implicitly claims it: a consultant should never be blocked by
        // a claim button they forgot to press.
        request.AssignedConsultantId ??= consultantId;

        var resources = SanitiseResources(input.Resources);
        var guidance = BuildGuidance(input.Guidance, resources);

        _context.ConsultationMessages.Add(new ConsultationMessage
        {
            ConsultationRequestId = request.Id,
            AuthorUserId = consultantId,
            AuthorRole = isAdmin && request.AssignedConsultantId != consultantId ? "Admin" : "Consultant",
            Body = input.Message.Trim(),
            IsInternal = false,
            ResourcesJson = JsonSerializer.Serialize(resources),
            CreatedAt = now
        });

        request.FirstRespondedAt ??= now;
        request.AnsweredAt = now;
        request.UpdatedAt = now;
        request.AgentDraftUsed = input.UsedAgentDraft;
        if (!string.IsNullOrWhiteSpace(input.ResolutionSummary))
        {
            request.ResolutionSummary = input.ResolutionSummary.Trim();
        }

        if (!string.IsNullOrWhiteSpace(guidance))
        {
            request.ConsultantGuidanceJson = guidance;
        }

        if (input.CloseAfterReply)
        {
            request.Status = ConsultationRequest.StatusClosed;
            request.ClosedAt = now;
        }
        else
        {
            request.Status = ConsultationRequest.StatusAnswered;
            request.ClosedAt = null;
        }

        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "Replied",
            FromStatus = previous,
            ToStatus = request.Status,
            Details = input.CloseAfterReply ? "Replied and closed." : "Replied to the student.",
            CreatedAt = now
        });

        // --- Write-back: the answer lands inside the component the student asked from -------------
        await ApplyWriteBackAsync(request, consultantId, guidance, resources, now);

        await _context.SaveChangesAsync();

        // --- Notification + optional email --------------------------------------------------------
        await PublishSafeAsync(new CreateNotificationDto
        {
            UserId = request.StudentId,
            Type = "ConsultationReplied",
            Title = "Your consultant replied",
            Body = $"{request.Subject} — open it to continue your pathway.",
            DeepLink = DeepLinkFor(request),
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Success",
            DedupeKey = $"consultation-reply:{request.Id}:{request.AnsweredAt?.Ticks}"
        });

        await TrySendEmailAsync(
            request.Student?.Email,
            "Pathway Navigator - your consultant replied",
            $"A consultant answered your question \"{request.Subject}\". Open the app to read the answer - your pathway is waiting where you left it.");

        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> AddInternalNoteAsync(Guid consultantId, bool isAdmin, Guid consultationId, string body)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        var now = DateTime.UtcNow;
        _context.ConsultationMessages.Add(new ConsultationMessage
        {
            ConsultationRequestId = request.Id,
            AuthorUserId = consultantId,
            AuthorRole = "Consultant",
            Body = body.Trim(),
            IsInternal = true,
            CreatedAt = now
        });

        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "InternalNoteAdded",
            FromStatus = request.Status,
            ToStatus = request.Status,
            Details = "Internal note added (not visible to the student).",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> UpdatePriorityAsync(Guid consultantId, bool isAdmin, Guid consultationId, string priority)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        var normalized = ConsultationService.NormalizePriority(priority);
        var now = DateTime.UtcNow;
        var previous = request.Priority;
        request.Priority = normalized;
        // The SLA clock is relative to the moment the case was raised, not to the priority change.
        request.SlaDueAt = ConsultationService.DueAt(normalized, request.CreatedAt);
        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "PriorityChanged",
            FromStatus = request.Status,
            ToStatus = request.Status,
            Details = $"Priority {previous} → {normalized}.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();
        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultationResponseDto?> EscalateAsync(Guid consultantId, bool isAdmin, Guid consultationId, string? reason)
    {
        var request = await _context.ConsultationRequests.SingleOrDefaultAsync(r => r.Id == consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin))
        {
            return null;
        }

        if (string.Equals(request.Status, ConsultationRequest.StatusEscalated, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This case is already escalated.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.Status = ConsultationRequest.StatusEscalated;
        request.UpdatedAt = now;
        request.AuditEvents.Add(new ConsultationAudit
        {
            ActorUserId = consultantId,
            Action = "Escalated",
            FromStatus = previous,
            ToStatus = ConsultationRequest.StatusEscalated,
            Details = string.IsNullOrWhiteSpace(reason) ? "Escalated to the counsellor queue." : reason.Trim(),
            CreatedAt = now
        });

        // Cross-role handoff: when the question is about a Reality Check, the counsellor who will decide
        // that review sees the consultant's note in the same evidence panel (audit trail, no schema change).
        if (request.ContextType == ConsultationRequest.ContextRealityCheck && request.ContextRefId.HasValue)
        {
            var review = await _context.PathwayReviews
                .SingleOrDefaultAsync(r => r.Id == request.ContextRefId.Value && r.StudentId == request.StudentId);
            if (review != null)
            {
                _context.PathwayReviewAudits.Add(new PathwayReviewAudit
                {
                    PathwayReviewId = review.Id,
                    ActorUserId = consultantId,
                    Action = "ConsultantEscalation",
                    FromStatus = review.Status,
                    ToStatus = review.Status,
                    Details = string.IsNullOrWhiteSpace(reason)
                        ? $"Consultant escalated the student's question: {request.Subject}"
                        : $"Consultant escalated: {reason.Trim()}",
                    CreatedAt = now
                });
            }
        }

        await _context.SaveChangesAsync();

        await PublishSafeAsync(new CreateNotificationDto
        {
            UserId = request.StudentId,
            Type = "ConsultationEscalated",
            Title = "Your question was escalated for an approval review",
            Body = request.Subject,
            DeepLink = DeepLinkFor(request),
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Warning"
        });

        var pool = new CreateNotificationDto
        {
            UserId = Guid.Empty,
            Type = "ConsultationEscalated",
            Title = "A consultant escalated a student question",
            Body = request.Subject,
            DeepLink = $"/counsellor/dashboard?consultation={request.Id}",
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Action",
            DedupeKey = $"consultation-escalated:{request.Id}"
        };
        await PublishToRoleSafeAsync("Counsellor", pool);
        await PublishToRoleSafeAsync("Admin", pool);

        return await _consultations.BuildResponseAsync(consultationId, consultantId, forStaff: true);
    }

    public async Task<ConsultantStatsDto> GetStatsAsync(Guid consultantId, bool isAdmin)
    {
        var now = DateTime.UtcNow;
        var weekAgo = now.AddDays(-7);

        var visible = _context.ConsultationRequests.AsNoTracking().AsQueryable();
        if (!isAdmin)
        {
            visible = visible.Where(r => r.AssignedConsultantId == consultantId);
        }

        var assigned = await visible.Where(r => r.ClosedAt == null).ToListAsync();
        var resolvedThisWeek = await visible.CountAsync(r => r.ClosedAt != null && r.ClosedAt >= weekAgo);

        var responded = await visible
            .Where(r => r.FirstRespondedAt != null && r.AnsweredAt != null)
            .Select(r => new { r.CreatedAt, First = r.FirstRespondedAt!.Value })
            .ToListAsync();
        var averageFirstResponse = responded.Count == 0
            ? 0
            : Math.Round(responded.Average(r => (r.First - r.CreatedAt).TotalHours), 2);

        var withSla = await visible
            .Where(r => r.FirstRespondedAt != null)
            .Select(r => new { r.SlaDueAt, First = r.FirstRespondedAt!.Value })
            .ToListAsync();
        var withinSla = withSla.Count(r => r.First <= r.SlaDueAt);
        var slaCompliance = withSla.Count == 0 ? 100d : Math.Round(withinSla * 100d / withSla.Count, 1);

        var rated = await visible.Where(r => r.StudentRating != null).Select(r => r.StudentRating!.Value).ToListAsync();

        var totalReplies = await _context.ConsultationMessages.AsNoTracking()
            .Where(m => !m.IsInternal && m.AuthorRole != "Student")
            .CountAsync();
        var draftAssisted = await visible.CountAsync(r => r.AgentDraftUsed);

        return new ConsultantStatsDto
        {
            OpenCases = assigned.Count,
            AssignedToMe = assigned.Count(r => r.AssignedConsultantId == consultantId),
            UnclaimedPool = await _context.ConsultationRequests.AsNoTracking()
                .CountAsync(r => r.AssignedConsultantId == null && r.ClosedAt == null),
            AwaitingStudent = assigned.Count(r => r.Status == ConsultationRequest.StatusAwaitingStudent),
            ResolvedThisWeek = resolvedThisWeek,
            AverageFirstResponseHours = averageFirstResponse,
            SlaCompliancePercent = slaCompliance,
            AverageRating = rated.Count == 0 ? 0 : Math.Round(rated.Average(), 2),
            RatedCount = rated.Count,
            DraftAssistedReplies = draftAssisted,
            DraftAcceptancePercent = totalReplies == 0 ? 0 : Math.Round(draftAssisted * 100d / totalReplies, 1)
        };
    }

    public async Task<AgentBriefResultDto?> GetBriefAsync(Guid consultantId, bool isAdmin, Guid consultationId)
    {
        var request = await LoadCaseAsync(consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin) || !_agentService.SupportsConsultationCopilot)
        {
            return null;
        }

        return await _agentService.ProcessAgent5BriefAsync(new AgentBriefRequestDto
        {
            Subject = request.Subject,
            Body = request.Body,
            ContextType = request.ContextType,
            ContextSummary = ConsultationService.SummariseContext(request.ContextSnapshotJson, request.ContextType),
            StudentEvidenceJson = request.ContextSnapshotJson
        });
    }

    public async Task<AgentDraftResultDto?> GetDraftAsync(Guid consultantId, bool isAdmin, Guid consultationId, DraftTone tone)
    {
        var request = await LoadCaseAsync(consultationId);
        if (request == null || !CanAccess(request, consultantId, isAdmin) || !_agentService.SupportsConsultationCopilot)
        {
            return null;
        }

        var toneName = tone switch
        {
            DraftTone.Direct => "Direct",
            DraftTone.Detailed => "Detailed",
            _ => "Supportive"
        };

        return await _agentService.ProcessAgent5DraftReplyAsync(new AgentDraftRequestDto
        {
            Subject = request.Subject,
            Body = request.Body,
            ContextType = request.ContextType,
            ContextSummary = ConsultationService.SummariseContext(request.ContextSnapshotJson, request.ContextType),
            StudentEvidenceJson = request.ContextSnapshotJson,
            Tone = toneName
        });
    }

    public async Task<ConsultantProfileDto?> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.ConsultantProfile)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            return null;
        }

        var openCases = await _context.ConsultationRequests.AsNoTracking()
            .CountAsync(r => r.AssignedConsultantId == userId && r.ClosedAt == null);
        var resolved = await _context.ConsultationRequests.AsNoTracking()
            .CountAsync(r => r.AssignedConsultantId == userId && r.ClosedAt != null);

        var profile = user.ConsultantProfile;
        return new ConsultantProfileDto
        {
            UserId = user.Id,
            Email = user.Email,
            FullName = user.FullName ?? string.Empty,
            Headline = profile?.Headline ?? string.Empty,
            Expertise = ParseList(profile?.ExpertiseJson),
            Languages = ParseList(profile?.LanguagesJson),
            IsAcceptingRequests = profile?.IsAcceptingRequests ?? true,
            IsActive = user.IsActive,
            MaxOpenCases = profile?.MaxOpenCases ?? 10,
            OpenCaseCount = openCases,
            ResolvedCount = resolved,
            CreatedAt = profile?.CreatedAt ?? user.CreatedAt
        };
    }

    public async Task<ConsultantProfileDto?> UpdateSelfAsync(Guid userId, UpdateConsultantSelfDto input)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.ConsultantProfile)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null || (user.Role?.Name != "Consultant" && user.Role?.Name != "Admin"))
        {
            return null;
        }

        var profile = user.ConsultantProfile;
        if (profile == null)
        {
            profile = new ConsultantProfile { UserId = user.Id };
            _context.ConsultantProfiles.Add(profile);
        }

        if (input.Headline != null) profile.Headline = input.Headline.Trim();
        if (input.Expertise != null) profile.ExpertiseJson = JsonSerializer.Serialize(Clean(input.Expertise));
        if (input.Languages != null) profile.LanguagesJson = JsonSerializer.Serialize(Clean(input.Languages));
        if (input.MaxOpenCases.HasValue) profile.MaxOpenCases = Math.Clamp(input.MaxOpenCases.Value, 1, 200);
        if (input.IsAcceptingRequests.HasValue) profile.IsAcceptingRequests = input.IsAcceptingRequests.Value;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetProfileAsync(userId);
    }

    // ------------------------------------------------------------------------------ internals

    private async Task<ConsultationRequest?> LoadCaseAsync(Guid consultationId) =>
        await _context.ConsultationRequests
            .Include(r => r.Student)
            .Include(r => r.AssignedConsultant)
            .SingleOrDefaultAsync(r => r.Id == consultationId);

    /// <summary>Pool cases are open to consultants; claimed cases only to their owner (or an Admin).</summary>
    private static bool CanAccess(ConsultationRequest request, Guid consultantId, bool isAdmin) =>
        isAdmin
        || request.AssignedConsultantId == consultantId
        || request.AssignedConsultantId == null;

    private static List<string> Clean(IEnumerable<string> values) =>
        values.Select(v => (v ?? string.Empty).Trim())
            .Where(v => v.Length is > 0 and <= 80)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(12)
            .ToList();

    private static List<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    /// <summary>
    /// Only http/https links survive: a resource can never become a <c>javascript:</c> payload rendered
    /// in the student's browser, and the label is never empty.
    /// </summary>
    internal static List<ConsultationResourceDto> SanitiseResources(IEnumerable<ConsultationResourceDto>? resources)
    {
        if (resources == null)
        {
            return new List<ConsultationResourceDto>();
        }

        var clean = new List<ConsultationResourceDto>();
        foreach (var resource in resources.Take(10))
        {
            var url = (resource.Url ?? string.Empty).Trim();
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                continue;
            }

            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                continue;
            }

            var label = string.IsNullOrWhiteSpace(resource.Label) ? uri.Host : resource.Label.Trim();
            clean.Add(new ConsultationResourceDto
            {
                Label = label.Length > 120 ? label.Substring(0, 120) : label,
                Url = url.Length > 500 ? url.Substring(0, 500) : url,
                Kind = string.IsNullOrWhiteSpace(resource.Kind) ? "guide" : resource.Kind.Trim().ToLowerInvariant()
            });
        }

        return clean;
    }

    private static string? BuildGuidance(ConsultationGuidanceDto? guidance, List<ConsultationResourceDto> messageResources)
    {
        if (guidance == null)
        {
            return null;
        }

        var payload = new
        {
            stageKey = string.IsNullOrWhiteSpace(guidance.StageKey) ? null : guidance.StageKey.Trim(),
            note = string.IsNullOrWhiteSpace(guidance.Note) ? null : Truncate(guidance.Note.Trim(), 1200),
            resources = SanitiseResources(guidance.Resources).Count > 0 ? SanitiseResources(guidance.Resources) : messageResources,
            checklist = guidance.Checklist.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => Truncate(c.Trim(), 200)).Take(10).ToList(),
            nextSteps = guidance.NextSteps.Where(c => !string.IsNullOrWhiteSpace(c)).Select(c => Truncate(c.Trim(), 200)).Take(10).ToList(),
            updatedAt = DateTime.UtcNow
        };

        return JsonSerializer.Serialize(payload);
    }

    private static string Truncate(string value, int max) => value.Length <= max ? value : value.Substring(0, max);

    /// <summary>
    /// Appends consultant guidance to the entity the student was looking at, so the answer renders
    /// inside that component (roadmap milestone / Reality Check panel) instead of only in an inbox.
    /// </summary>
    private async Task ApplyWriteBackAsync(
        ConsultationRequest request,
        Guid consultantId,
        string? guidanceJson,
        List<ConsultationResourceDto> resources,
        DateTime now)
    {
        var consultantName = await _context.Users.AsNoTracking()
            .Where(u => u.Id == consultantId)
            .Select(u => u.FullName ?? u.Email)
            .FirstOrDefaultAsync() ?? "Consultant";

        if (request.ContextType == ConsultationRequest.ContextPathwayPlan && request.ContextRefId.HasValue)
        {
            var plan = await _context.PathwayPlans
                .SingleOrDefaultAsync(p => p.Id == request.ContextRefId.Value && p.StudentProfile!.UserId == request.StudentId);
            if (plan != null)
            {
                var item = new
                {
                    stageKey = ReadStageKey(guidanceJson),
                    consultationId = request.Id,
                    consultantName,
                    note = ReadNote(guidanceJson) ?? request.ResolutionSummary,
                    resources,
                    checklist = ReadStringArray(guidanceJson, "checklist"),
                    createdAt = now
                };
                plan.ConsultantGuidanceJson = AppendToJsonArray(plan.ConsultantGuidanceJson, JsonSerializer.Serialize(item));
                plan.UpdatedAt = now;
            }
        }
        else if (request.ContextType == ConsultationRequest.ContextRealityCheck && request.ContextRefId.HasValue)
        {
            var review = await _context.PathwayReviews
                .SingleOrDefaultAsync(r => r.Id == request.ContextRefId.Value && r.StudentId == request.StudentId);
            if (review != null)
            {
                var item = new
                {
                    consultationId = request.Id,
                    consultantName,
                    note = ReadNote(guidanceJson) ?? request.ResolutionSummary,
                    resources,
                    createdAt = now
                };
                review.ConsultantAdviceJson = AppendToJsonArray(review.ConsultantAdviceJson, JsonSerializer.Serialize(item));
                review.UpdatedAt = now;
            }
        }
    }

    private static string? ReadStageKey(string? guidanceJson) => ReadString(guidanceJson, "stageKey");

    private static string? ReadNote(string? guidanceJson) => ReadString(guidanceJson, "note");

    private static string? ReadString(string? guidanceJson, string property)
    {
        if (string.IsNullOrWhiteSpace(guidanceJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(guidanceJson);
            return document.RootElement.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static List<string> ReadStringArray(string? guidanceJson, string property)
    {
        if (string.IsNullOrWhiteSpace(guidanceJson))
        {
            return new List<string>();
        }

        try
        {
            using var document = JsonDocument.Parse(guidanceJson);
            if (!document.RootElement.TryGetProperty(property, out var value) || value.ValueKind != JsonValueKind.Array)
            {
                return new List<string>();
            }

            return value.EnumerateArray()
                .Where(v => v.ValueKind == JsonValueKind.String)
                .Select(v => v.GetString() ?? string.Empty)
                .Where(v => v.Length > 0)
                .ToList();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }

    /// <summary>Appends to a JSON array string, keeping the newest N items and never throwing on bad input.</summary>
    internal static string AppendToJsonArray(string? existingJson, string newItemJson)
    {
        var items = new List<string>();
        if (!string.IsNullOrWhiteSpace(existingJson))
        {
            try
            {
                using var document = JsonDocument.Parse(existingJson);
                if (document.RootElement.ValueKind == JsonValueKind.Array)
                {
                    items.AddRange(document.RootElement.EnumerateArray().Select(element => element.GetRawText()));
                }
            }
            catch (JsonException)
            {
                items.Clear();
            }
        }

        items.Add(newItemJson);
        if (items.Count > MaxGuidanceEntries)
        {
            items = items.Skip(items.Count - MaxGuidanceEntries).ToList();
        }

        return "[" + string.Join(",", items) + "]";
    }

    /// <summary>Deep link that returns the student to the exact panel they asked from.</summary>
    internal static string DeepLinkFor(ConsultationRequest request) => request.ContextType switch
    {
        ConsultationRequest.ContextCareerDiscovery =>
            $"/career-discovery?consultation={request.Id}&focus=pathway:{request.ContextRefId}",
        ConsultationRequest.ContextPathwayPlan =>
            $"/career-discovery?consultation={request.Id}&focus=plan:{request.ContextRefId}",
        ConsultationRequest.ContextRealityCheck =>
            $"/student/reality-check?consultation={request.Id}",
        _ => $"/student/support?consultation={request.Id}"
    };

    private async Task PublishSafeAsync(CreateNotificationDto notification)
    {
        try
        {
            await _notifications.PublishAsync(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification {Type} could not be published.", notification.Type);
        }
    }

    private async Task PublishToRoleSafeAsync(string role, CreateNotificationDto notification)
    {
        try
        {
            await _notifications.PublishToRoleAsync(role, notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Role notification {Type} could not be published.", notification.Type);
        }
    }

    private async Task TrySendEmailAsync(string? email, string subject, string body)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return;
        }

        try
        {
            await _emailService.SendConsultationUpdateAsync(email, subject, body);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Consultation notification email could not be sent.");
        }
    }
}
