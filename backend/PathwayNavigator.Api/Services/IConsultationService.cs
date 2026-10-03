using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

/// <summary>Student-facing side of the consultation workflow.</summary>
public interface IConsultationService
{
    Task<ConsultationResponseDto> CreateAsync(Guid studentId, CreateConsultationRequestDto input);

    Task<PagedConsultationsDto> GetMineAsync(Guid studentId, string? status, int page, int pageSize);

    /// <summary>Full thread including messages. Returns null when the request is not the student's.</summary>
    Task<ConsultationResponseDto?> GetForStudentAsync(Guid studentId, Guid consultationId);

    Task<ConsultationResponseDto?> AddStudentMessageAsync(Guid studentId, Guid consultationId, string body);

    Task<ConsultationResponseDto?> CloseAsync(Guid studentId, Guid consultationId, CloseConsultationDto input);

    Task<ConsultationResponseDto?> ReopenAsync(Guid studentId, Guid consultationId);

    /// <summary>Lets a journey page show "you already asked about this" instead of a duplicate form.</summary>
    Task<ConsultationContextCheckDto> GetContextStatusAsync(Guid studentId, string contextType, Guid? contextRefId);

    Task<AgentFaqMatchResultDto?> MatchFaqAsync(string question);

    /// <summary>
    /// Full staff view (internal notes and triage evidence included). Returns null when the
    /// consultation does not exist, or is not the requester's when <paramref name="forStaff"/> is false.
    /// Used by the consultant desk so every endpoint maps through the same choke point.
    /// </summary>
    Task<ConsultationResponseDto?> BuildResponseAsync(Guid consultationId, Guid requesterId, bool forStaff);

    /// <summary>Maps an already-loaded entity; lets the queue map a whole page from one query.</summary>
    Task<ConsultationResponseDto> MapAsync(ConsultationRequest request, bool forStaff);
}
