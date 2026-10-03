using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Consultation;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.DTOs.Review;

namespace PathwayNavigator.Api.Services
{
    public interface IAgentService
    {
        Task<AgentChatResponseDto?> ProcessAgent1TurnAsync(AgentChatRequestDto request, string? userId = null);

        Task<PathwayAnalysisResponseDto?> ProcessAgent2AnalysisAsync(StudentProfileDto profile, string? userId = null);

        Task<RealityCheckAgentResponseDto?> ProcessAgent4RealityCheckAsync(RealityCheckAgentRequestDto request);
        Task<PathwayPlannerResponseDto?> ProcessAgent3PlanAsync(PathwayPlannerRequestDto request, string? userId = null);

        /// <summary>
        /// False when the consultant copilot is switched off (or unconfigured). Callers use this to skip
        /// Agent 5 entirely instead of absorbing a guaranteed failure - the consultation workflow must
        /// work with the AI service offline.
        /// </summary>
        bool SupportsConsultationCopilot { get; }

        Task<AgentTriageResultDto?> ProcessAgent5TriageAsync(AgentTriageRequestDto request);
        Task<AgentBriefResultDto?> ProcessAgent5BriefAsync(AgentBriefRequestDto request);
        Task<AgentDraftResultDto?> ProcessAgent5DraftReplyAsync(AgentDraftRequestDto request);
        Task<AgentFaqMatchResultDto?> ProcessAgent5FaqMatchAsync(AgentFaqMatchRequestDto request);
    }
}
