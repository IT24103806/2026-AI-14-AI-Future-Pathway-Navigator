using PathwayNavigator.Api.DTOs.Consultant;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Admin-only provisioning of Consultant accounts.
///
/// This exists because self-service registration is now Student-only (`AuthService.RegisterAsync`).
/// Without an explicit provisioning path there would be no way to create the staff account the
/// consultant desk depends on.
/// </summary>
public interface IConsultantAccountService
{
    Task<IReadOnlyList<ConsultantProfileDto>> GetAllAsync();

    Task<ConsultantProfileDto> CreateAsync(CreateConsultantDto input);

    Task<ConsultantProfileDto?> UpdateAsync(Guid userId, UpdateConsultantDto input);

    Task<ConversationMetricsDto> GetMetricsAsync();
}
