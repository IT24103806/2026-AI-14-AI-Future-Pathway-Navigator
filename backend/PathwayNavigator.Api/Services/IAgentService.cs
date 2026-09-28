using System.Threading.Tasks;
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
    }
}
