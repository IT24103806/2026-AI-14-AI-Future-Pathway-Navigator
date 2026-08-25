using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Onboarding;

namespace PathwayNavigator.Api.Services
{
    public interface IAgentService
    {
        Task<AgentChatResponseDto?> ProcessAgent1TurnAsync(AgentChatRequestDto request, string? userId = null);
    }
}
