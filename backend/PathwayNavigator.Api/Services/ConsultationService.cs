using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.DTOs.Notification;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Student side of the consultant workflow: raise a context-anchored request, follow the thread,
/// confirm resolution or reopen. Every state change writes a <see cref="ConsultationAudit"/> row and
/// publishes a notification, so the student never has to poll to discover that someone answered.
/// </summary>
public class ConsultationService : IConsultationService
{
    /// <summary>Backstop against flooding the queue; the per-minute cooldown is enforced in CreateAsync.</summary>
    internal const int MaxOpenRequestsPerStudent = 3;
    internal static readonly TimeSpan CreationCooldown = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan ReopenWindow = TimeSpan.FromDays(7);

    private static readonly Dictionary<string, int> SlaHoursByPriority = new(StringComparer.OrdinalIgnoreCase)
    {
        ["P1"] = 24,
        ["P2"] = 48,
        ["P3"] = 72
    };

    private readonly AppDbContext _context;
    private readonly IContextSnapshotResolver _snapshotResolver;
    private readonly INotificationPublisher _notifications;
    private readonly IAgentService _agentService;
    private readonly ILogger<ConsultationService> _logger;

    public ConsultationService(
        AppDbContext context,
        IContextSnapshotResolver snapshotResolver,
        INotificationPublisher notifications,
        IAgentService agentService,
        ILogger<ConsultationService> logger)
    {
        _context = context;
        _snapshotResolver = snapshotResolver;
        _notifications = notifications;
        _agentService = agentService;
        _logger = logger;
    }

    public static DateTime DueAt(string priority, DateTime from) =>
        from.AddHours(SlaHoursByPriority.TryGetValue(priority, out var hours) ? hours : 48);

    public async Task<ConsultationResponseDto> CreateAsync(Guid studentId, CreateConsultationRequestDto input)
    {
        var now = DateTime.UtcNow;

        // --- Abuse controls ---------------------------------------------------------------------
        var openCount = await _context.ConsultationRequests
            .CountAsync(r => r.StudentId == studentId && r.ClosedAt == null);
        if (openCount >= MaxOpenRequestsPerStudent)
        {
            throw new InvalidOperationException(
                $"You already have {MaxOpenRequestsPerStudent} open questions. Please close one before asking another.");
        }

        var lastCreated = await _context.ConsultationRequests
            .Where(r => r.StudentId == studentId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => (DateTime?)r.CreatedAt)
            .FirstOrDefaultAsync();
        if (lastCreated.HasValue && now - lastCreated.Value < CreationCooldown)
        {
            throw new InvalidOperationException("Please wait a moment before sending another question.");
        }

        // --- Context anchoring (also enforces ownership of the referenced record) ---------------
        var snapshot = await _snapshotResolver.ResolveAsync(studentId, input.ContextType, input.ContextRefId);

        // --- Agent 5 triage is best-effort: it can improve routing, never block the request ------
        AgentTriageResultDto? triage = null;
        if (_agentService.SupportsConsultationCopilot)
        {
            try
            {
                triage = await _agentService.ProcessAgent5TriageAsync(new AgentTriageRequestDto
                {
                    Subject = input.Subject.Trim(),
                    Body = input.Body.Trim(),
                    ContextType = snapshot.ContextType,
                    ContextSummary = snapshot.Summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Agent 5 triage was unavailable; the request is stored untriaged.");
            }
        }

        var category = FirstNonEmpty(
            triage?.Category,
            input.Category,
            "Unclassified");
        var priority = NormalizePriority(FirstNonEmpty(triage?.Priority, input.Priority, "P2"));

        var request = new ConsultationRequest
        {
            StudentId = studentId,
            ContextType = snapshot.ContextType,
            ContextRefId = snapshot.ContextRefId,
            ContextSnapshotJson = snapshot.Json,
            Category = category,
            Priority = priority,
            Subject = input.Subject.Trim(),
            Body = input.Body.Trim(),
            Status = ConsultationRequest.StatusOpen,
            SlaDueAt = DueAt(priority, now),
            AgentTriageJson = triage == null ? null : JsonSerializer.Serialize(triage),
            CreatedAt = now,
            UpdatedAt = now
        };

        _context.ConsultationRequests.Add(request);
        _context.ConsultationAudits.Add(new ConsultationAudit
        {
            ConsultationRequestId = request.Id,
            ActorUserId = studentId,
            Action = "Created",
            FromStatus = "None",
            ToStatus = ConsultationRequest.StatusOpen,
            Details = $"Raised from {snapshot.ContextType}. Triage: {category}/{priority}.",
            CreatedAt = now
        });

        // The request and its audit row are one unit of work. The notification is intentionally written
        // afterwards so a notification failure can never lose the student's question.
        await _context.SaveChangesAsync();

        await PublishToRoleAsyncSafe("Consultant", new CreateNotificationDto
        {
            UserId = Guid.Empty, // replaced per recipient by PublishToRoleAsync
            Type = "ConsultationSubmitted",
            Title = $"New {priority} question: {request.Subject}",
            Body = $"{category} · {snapshot.Summary}",
            DeepLink = $"/consultant/dashboard?consultation={request.Id}",
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = priority == "P1" ? "Action" : "Info",
            DedupeKey = $"consultation-submitted:{request.Id}"
        });

        await PublishToRoleAsyncSafe("Admin", new CreateNotificationDto
        {
            UserId = Guid.Empty,
            Type = "ConsultationSubmitted",
            Title = $"New {priority} student question",
            Body = $"{category} · {snapshot.Summary}",
            DeepLink = $"/admin/dashboard?consultation={request.Id}",
            EntityType = "Consultation",
            EntityId = request.Id,
            Priority = "Info",
            DedupeKey = $"consultation-submitted-admin:{request.Id}"
        });

        return await BuildResponseAsync(request.Id, studentId, forStaff: false) ?? throw new InvalidOperationException(
            "The consultation could not be read back after creation.");
    }

    public async Task<PagedConsultationsDto> GetMineAsync(Guid studentId, string? status, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.ConsultationRequests.AsNoTracking().Where(r => r.StudentId == studentId);
        if (!string.IsNullOrWhiteSpace(status) && !string.Equals(status, "All", StringComparison.OrdinalIgnoreCase))
        {
            if (!AllowedStatuses.Contains(status))
            {
                throw new ArgumentException("Invalid consultation status.");
            }

            query = query.Where(r => r.Status == status);
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(r => r.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<ConsultationResponseDto>(rows.Count);
        foreach (var row in rows)
        {
            items.Add(await MapAsync(row, forStaff: false));
        }

        return new PagedConsultationsDto { Items = items, Page = page, PageSize = pageSize, TotalCount = total };
    }

    public async Task<ConsultationResponseDto?> GetForStudentAsync(Guid studentId, Guid consultationId) =>
        await BuildResponseAsync(consultationId, studentId, forStaff: false);

    public async Task<ConsultationResponseDto?> AddStudentMessageAsync(Guid studentId, Guid consultationId, string body)
    {
        var request = await _context.ConsultationRequests
            .SingleOrDefaultAsync(r => r.Id == consultationId && r.StudentId == studentId);

        if (request == null)
        {
            return null;
        }

        if (string.Equals(request.Status, ConsultationRequest.StatusClosed, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This question is closed. Reopen it to continue the conversation.");
        }

        var now = DateTime.UtcNow;
        _context.ConsultationMessages.Add(new ConsultationMessage
        {
            ConsultationRequestId = request.Id,
            AuthorUserId = studentId,
            AuthorRole = "Student",
            Body = body.Trim(),
            CreatedAt = now
        });

        var previous = request.Status;
        // A student reply moves a case back into the consultant's court.
        request.Status = previous switch
        {
            ConsultationRequest.StatusAnswered => ConsultationRequest.StatusInProgress,
            ConsultationRequest.StatusAwaitingStudent => ConsultationRequest.StatusInProgress,
            ConsultationRequest.StatusResolved => ConsultationRequest.StatusInProgress,
            _ => previous
        };
        request.UpdatedAt = now;

        _context.ConsultationAudits.Add(new ConsultationAudit
        {
            ConsultationRequestId = request.Id,
            ActorUserId = studentId,
            Action = "StudentReplied",
            FromStatus = previous,
            ToStatus = request.Status,
            Details = "The student added more information.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        if (request.AssignedConsultantId.HasValue)
        {
            await PublishSafeAsync(new CreateNotificationDto
            {
                UserId = request.AssignedConsultantId.Value,
                Type = "ConsultationStudentReplied",
                Title = "The student replied",
                Body = request.Subject,
                DeepLink = $"/consultant/dashboard?consultation={request.Id}",
                EntityType = "Consultation",
                EntityId = request.Id,
                Priority = "Info"
            });
        }

        return await BuildResponseAsync(request.Id, studentId, forStaff: false);
    }

    public async Task<ConsultationResponseDto?> CloseAsync(Guid studentId, Guid consultationId, CloseConsultationDto input)
    {
        var request = await _context.ConsultationRequests
            .SingleOrDefaultAsync(r => r.Id == consultationId && r.StudentId == studentId);

        if (request == null)
        {
            return null;
        }

        if (request.ClosedAt != null)
        {
            throw new InvalidOperationException("This question is already closed.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.Status = ConsultationRequest.StatusClosed;
        request.ClosedAt = now;
        request.UpdatedAt = now;
        request.StudentRating = input.Rating;
        request.StudentFeedback = string.IsNullOrWhiteSpace(input.Feedback) ? null : input.Feedback.Trim();

        _context.ConsultationAudits.Add(new ConsultationAudit
        {
            ConsultationRequestId = request.Id,
            ActorUserId = studentId,
            Action = "Closed",
            FromStatus = previous,
            ToStatus = ConsultationRequest.StatusClosed,
            Details = input.Rating.HasValue ? $"Student confirmed resolution (rating {input.Rating}/5)." : "Student closed the question.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        if (request.AssignedConsultantId.HasValue)
        {
            await PublishSafeAsync(new CreateNotificationDto
            {
                UserId = request.AssignedConsultantId.Value,
                Type = "ConsultationClosed",
                Title = "Student closed a question",
                Body = input.Rating.HasValue ? $"{request.Subject} · rated {input.Rating}/5" : request.Subject,
                DeepLink = $"/consultant/dashboard?consultation={request.Id}",
                EntityType = "Consultation",
                EntityId = request.Id,
                Priority = "Info"
            });
        }

        return await BuildResponseAsync(request.Id, studentId, forStaff: false);
    }

    public async Task<ConsultationResponseDto?> ReopenAsync(Guid studentId, Guid consultationId)
    {
        var request = await _context.ConsultationRequests
            .SingleOrDefaultAsync(r => r.Id == consultationId && r.StudentId == studentId);

        if (request == null)
        {
            return null;
        }

        if (request.ClosedAt == null)
        {
            throw new InvalidOperationException("This question is not closed.");
        }

        if (DateTime.UtcNow - request.ClosedAt.Value > ReopenWindow)
        {
            throw new InvalidOperationException("This question can only be reopened within 7 days of closing. Raise a new one instead.");
        }

        var now = DateTime.UtcNow;
        var previous = request.Status;
        request.Status = request.AssignedConsultantId.HasValue
            ? ConsultationRequest.StatusInProgress
            : ConsultationRequest.StatusOpen;
        request.ClosedAt = null;
        request.UpdatedAt = now;
        // A reopened request must be worked again, so it gets a fresh SLA clock.
        request.SlaDueAt = DueAt(request.Priority, now);

        _context.ConsultationAudits.Add(new ConsultationAudit
        {
            ConsultationRequestId = request.Id,
            ActorUserId = studentId,
            Action = "Reopened",
            FromStatus = previous,
            ToStatus = request.Status,
            Details = "The student reopened the question.",
            CreatedAt = now
        });

        await _context.SaveChangesAsync();

        if (request.AssignedConsultantId.HasValue)
        {
            await PublishSafeAsync(new CreateNotificationDto
            {
                UserId = request.AssignedConsultantId.Value,
                Type = "ConsultationReopened",
                Title = "A question was reopened",
                Body = request.Subject,
                DeepLink = $"/consultant/dashboard?consultation={request.Id}",
                EntityType = "Consultation",
                EntityId = request.Id,
                Priority = "Info"
            });
        }

        return await BuildResponseAsync(request.Id, studentId, forStaff: false);
    }

    public async Task<ConsultationContextCheckDto> GetContextStatusAsync(Guid studentId, string contextType, Guid? contextRefId)
    {
        if (contextRefId == null)
        {
            return new ConsultationContextCheckDto();
        }

        var existing = await _context.ConsultationRequests.AsNoTracking()
            .Where(r => r.StudentId == studentId
                        && r.ContextType == contextType
                        && r.ContextRefId == contextRefId
                        && r.ClosedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync();

        return existing == null
            ? new ConsultationContextCheckDto()
            : new ConsultationContextCheckDto
            {
                HasOpenRequest = true,
                ConsultationId = existing.Id,
                Status = existing.Status,
                Subject = existing.Subject
            };
    }

    public async Task<AgentFaqMatchResultDto?> MatchFaqAsync(string question) =>
        _agentService.SupportsConsultationCopilot
            ? await _agentService.ProcessAgent5FaqMatchAsync(new AgentFaqMatchRequestDto { Question = question })
            : null;

    // ----------------------------------------------------------------------------- helpers

    internal static readonly HashSet<string> AllowedStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        ConsultationRequest.StatusOpen,
        ConsultationRequest.StatusClaimed,
        ConsultationRequest.StatusInProgress,
        ConsultationRequest.StatusAwaitingStudent,
        ConsultationRequest.StatusAnswered,
        ConsultationRequest.StatusResolved,
        ConsultationRequest.StatusClosed,
        ConsultationRequest.StatusEscalated
    };

    internal static string NormalizePriority(string? priority)
    {
        var value = (priority ?? "P2").Trim().ToUpperInvariant();
        return value is "P1" or "P2" or "P3" ? value : "P2";
    }

    private static string FirstNonEmpty(params string?[] values) =>
        values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim() ?? string.Empty;

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

    private async Task PublishToRoleAsyncSafe(string role, CreateNotificationDto notification)
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

    public async Task<ConsultationResponseDto?> BuildResponseAsync(Guid consultationId, Guid requesterId, bool forStaff)
    {
        // Student and message Author have non-nullable Guid foreign keys, which EF Core's InMemory
        // and relational providers translate as INNER JOINs when Included. Resolving User display
        // names in MapInternalAsync guarantees a request or message is never filtered out when a
        // unit test seeds a bare UserId without a matching Users row.
        var query = _context.ConsultationRequests.AsNoTracking()
            .Include(r => r.Messages)
            .Include(r => r.AuditEvents)
            .AsQueryable();

        var request = forStaff
            ? await query.SingleOrDefaultAsync(r => r.Id == consultationId)
            : await query.SingleOrDefaultAsync(r => r.Id == consultationId && r.StudentId == requesterId);

        if (request == null)
        {
            return null;
        }

        return await MapAsync(request, forStaff);
    }

    public Task<ConsultationResponseDto> MapAsync(ConsultationRequest request, bool forStaff) =>
        MapInternalAsync(request, forStaff);

    private async Task<ConsultationResponseDto> MapInternalAsync(ConsultationRequest request, bool forStaff)
    {
        var messages = request.Messages ?? new List<ConsultationMessage>();

        var userIds = new HashSet<Guid> { request.StudentId };
        if (request.AssignedConsultantId.HasValue)
        {
            userIds.Add(request.AssignedConsultantId.Value);
        }

        foreach (var message in messages)
        {
            userIds.Add(message.AuthorUserId);
        }

        var usersById = await _context.Users.AsNoTracking()
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        User? ResolveUser(Guid? id, User? navigation) =>
            navigation ?? (id.HasValue && usersById.TryGetValue(id.Value, out var found) ? found : null);

        var student = ResolveUser(request.StudentId, request.Student);
        var assignedConsultant = ResolveUser(request.AssignedConsultantId, request.AssignedConsultant);

        var dto = new ConsultationResponseDto
        {
            Id = request.Id,
            StudentId = request.StudentId,
            StudentName = DisplayName(student),
            AssignedConsultantId = request.AssignedConsultantId,
            AssignedConsultantName = request.AssignedConsultantId == null ? null : DisplayName(assignedConsultant),
            ContextType = request.ContextType,
            ContextRefId = request.ContextRefId,
            ContextSnapshotJson = request.ContextSnapshotJson,
            ContextSummary = SummariseContext(request.ContextSnapshotJson, request.ContextType),
            Category = request.Category,
            Priority = request.Priority,
            Subject = request.Subject,
            Body = request.Body,
            Status = request.Status,
            SlaDueAt = request.SlaDueAt,
            IsSlaBreached = request.ClosedAt == null && DateTime.UtcNow > request.SlaDueAt,
            FirstRespondedAt = request.FirstRespondedAt,
            AnsweredAt = request.AnsweredAt,
            ClosedAt = request.ClosedAt,
            ResolutionSummary = request.ResolutionSummary,
            ConsultantGuidanceJson = request.ConsultantGuidanceJson,
            AgentDraftUsed = request.AgentDraftUsed,
            StudentRating = request.StudentRating,
            StudentFeedback = request.StudentFeedback,
            CreatedAt = request.CreatedAt,
            UpdatedAt = request.UpdatedAt,
            AuditEvents = (request.AuditEvents ?? new List<ConsultationAudit>())
                .OrderBy(a => a.CreatedAt)
                .Select(a => new ConsultationAuditDto
                {
                    Id = a.Id,
                    Action = a.Action,
                    FromStatus = a.FromStatus,
                    ToStatus = a.ToStatus,
                    Details = a.Details,
                    CreatedAt = a.CreatedAt
                }).ToList()
        };

        // Internal notes are filtered here - the single choke point that guarantees a student can
        // never read a consultant's working note, regardless of which endpoint produced the DTO.
        dto.Messages = messages
            .Where(m => forStaff || !m.IsInternal)
            .Where(m => m.DeletedAt == null)
            .OrderBy(m => m.CreatedAt)
            .Select(m =>
            {
                var author = ResolveUser(m.AuthorUserId, m.Author);
                return new ConsultationMessageDto
                {
                    Id = m.Id,
                    AuthorUserId = m.AuthorUserId,
                    AuthorName = m.IsInternal ? $"{DisplayName(author)} (internal)" : DisplayName(author),
                    AuthorRole = m.AuthorRole,
                    Body = m.Body,
                    IsInternal = m.IsInternal,
                    Resources = ParseResources(m.ResourcesJson),
                    CreatedAt = m.CreatedAt,
                    EditedAt = m.EditedAt
                };
            }).ToList();

        if (forStaff && !string.IsNullOrWhiteSpace(request.AgentTriageJson))
        {
            try
            {
                dto.AgentTriage = JsonSerializer.Deserialize<AgentTriageResultDto>(request.AgentTriageJson);
            }
            catch (JsonException)
            {
                dto.AgentTriage = null;
            }
        }

        return await Task.FromResult(dto);
    }

    private static string DisplayName(User? user)
    {
        if (user == null)
        {
            return "Unknown user";
        }

        if (!string.IsNullOrWhiteSpace(user.FullName))
        {
            return user.FullName!.Trim();
        }

        // Never expose a full email address to the other side of the conversation.
        var email = user.Email ?? string.Empty;
        var at = email.IndexOf('@');
        return at > 0 ? email.Substring(0, at) : email;
    }

    internal static List<ConsultationResourceDto> ParseResources(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ConsultationResourceDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ConsultationResourceDto>>(json,
                       new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                   ?? new List<ConsultationResourceDto>();
        }
        catch (JsonException)
        {
            return new List<ConsultationResourceDto>();
        }
    }

    /// <summary>
    /// Human-readable context line rendered by both clients. Falls back to a neutral label when the
    /// frozen snapshot cannot be read, so the queue never shows a raw JSON blob.
    /// </summary>
    internal static string SummariseContext(string? snapshotJson, string contextType)
    {
        if (string.IsNullOrWhiteSpace(snapshotJson))
        {
            return contextType;
        }

        try
        {
            using var document = JsonDocument.Parse(snapshotJson);
            var root = document.RootElement;

            string? Read(params string[] names)
            {
                foreach (var name in names)
                {
                    if (root.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null)
                    {
                        if (value.ValueKind == JsonValueKind.String)
                        {
                            var text = value.GetString();
                            if (!string.IsNullOrWhiteSpace(text)) return text;
                        }
                        else if (value.ValueKind == JsonValueKind.Number)
                        {
                            return value.ToString();
                        }
                    }
                }

                return null;
            }

            return contextType switch
            {
                ConsultationRequest.ContextCareerDiscovery =>
                    Read("status") != null ? $"Career Discovery · {Read("status")}" : "Career Discovery",
                ConsultationRequest.ContextPathwayPlan =>
                    Read("selectedPathway") != null ? $"Roadmap · {Read("selectedPathway")}" : "Roadmap",
                ConsultationRequest.ContextRealityCheck =>
                    Read("targetCareer") != null
                        ? $"Reality Check · {Read("targetCareer")} · {Read("status")} · {Read("feasibilityScore")}%"
                        : "Reality Check",
                _ => "General question"
            };
        }
        catch (JsonException)
        {
            return contextType;
        }
    }
}
