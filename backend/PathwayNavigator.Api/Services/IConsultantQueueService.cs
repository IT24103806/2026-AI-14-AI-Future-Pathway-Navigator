using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.DTOs.Consultant;

namespace PathwayNavigator.Api.Services;

/// <summary>Consultant-desk side of the consultation workflow.</summary>
public interface IConsultantQueueService
{
    Task<PagedConsultationsDto> GetQueueAsync(Guid consultantId, bool isAdmin, ConsultantQueueFilter filter);

    Task<ConsultationResponseDto?> GetCaseAsync(Guid consultantId, bool isAdmin, Guid consultationId);

    Task<ConsultationResponseDto?> ClaimAsync(Guid consultantId, Guid consultationId);

    Task<ConsultationResponseDto?> ReleaseAsync(Guid consultantId, bool isAdmin, Guid consultationId, string? reason);

    Task<ConsultationResponseDto?> ReassignAsync(Guid adminId, Guid consultationId, Guid consultantUserId);

    Task<ConsultationResponseDto?> ReplyAsync(Guid consultantId, bool isAdmin, Guid consultationId, ReplyConsultationDto input);

    Task<ConsultationResponseDto?> AddInternalNoteAsync(Guid consultantId, bool isAdmin, Guid consultationId, string body);

    Task<ConsultationResponseDto?> UpdatePriorityAsync(Guid consultantId, bool isAdmin, Guid consultationId, string priority);

    Task<ConsultationResponseDto?> EscalateAsync(Guid consultantId, bool isAdmin, Guid consultationId, string? reason);

    Task<ConsultantStatsDto> GetStatsAsync(Guid consultantId, bool isAdmin);

    Task<AgentBriefResultDto?> GetBriefAsync(Guid consultantId, bool isAdmin, Guid consultationId);

    Task<AgentDraftResultDto?> GetDraftAsync(Guid consultantId, bool isAdmin, Guid consultationId, DraftTone tone);

    Task<ConsultantProfileDto?> GetProfileAsync(Guid userId);

    Task<ConsultantProfileDto?> UpdateSelfAsync(Guid userId, UpdateConsultantSelfDto input);
}

public class ConsultantQueueFilter
{
    /// <summary>Open (pool) | Mine | All | plus any concrete request status.</summary>
    public string Scope { get; set; } = "Open";
    public string? Category { get; set; }
    public string? Priority { get; set; }
    public string? ContextType { get; set; }
    public string? Search { get; set; }
    public string Sort { get; set; } = "sla";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}
