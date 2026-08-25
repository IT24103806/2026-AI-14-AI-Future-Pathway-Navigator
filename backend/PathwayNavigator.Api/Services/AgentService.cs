using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PathwayNavigator.Api.DTOs.Onboarding;

namespace PathwayNavigator.Api.Services
{
    public class AgentService : IAgentService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AgentService> _logger;

        public AgentService(HttpClient httpClient, IConfiguration configuration, ILogger<AgentService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var baseUrl = _configuration["AiServiceSettings:BaseUrl"] ?? "http://localhost:8000";
            if (_httpClient.BaseAddress == null)
            {
                _httpClient.BaseAddress = new Uri(baseUrl);
            }
        }

        public async Task<AgentChatResponseDto?> ProcessAgent1TurnAsync(AgentChatRequestDto request, string? userId = null)
        {
            try
            {
                var payload = new
                {
                    user_id = userId,
                    message = request.Message,
                    history = request.History,
                    current_slots = request.CurrentSlots
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/agent-1/chat", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AgentChatResponseDto>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI Microservice returned error status {StatusCode}: {ErrorText}", response.StatusCode, errorText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with AI microservice at {BaseAddress}", _httpClient.BaseAddress);
            }

            // Fallback response if AI service is temporarily offline during development
            return GenerateFallbackTurn(request);
        }

        private AgentChatResponseDto GenerateFallbackTurn(AgentChatRequestDto request)
        {
            var slots = request.CurrentSlots ?? new ExtractedSlotsDto();
            var text = request.Message.ToLower();

            if (text.Contains("o/l")) slots.AcademicStage = "After O/L";
            else if (text.Contains("a/l")) slots.AcademicStage = "After A/L";
            else if (text.Contains("undergraduate") || text.Contains("bachelor") || text.Contains("sliit")) slots.AcademicStage = "Undergraduate";
            else if (text.Contains("graduate")) slots.AcademicStage = "Graduated";

            if (text.Contains("python") && !slots.CoreSkills.Contains("Python")) slots.CoreSkills.Add("Python");
            if (text.Contains("java") && !slots.CoreSkills.Contains("Java")) slots.CoreSkills.Add("Java");
            if (text.Contains("web") && !slots.CoreSkills.Contains("Web Development")) slots.CoreSkills.Add("Web Development");
            if (text.Contains("design") && !slots.CoreSkills.Contains("UI/UX")) slots.CoreSkills.Add("UI/UX");

            if (text.Contains("robotics") && !slots.HobbiesInterests.Contains("Robotics")) slots.HobbiesInterests.Add("Robotics");
            if (text.Contains("gaming") && !slots.HobbiesInterests.Contains("Gaming")) slots.HobbiesInterests.Add("Gaming");
            if (text.Contains("ai") && !slots.HobbiesInterests.Contains("AI")) slots.HobbiesInterests.Add("AI");

            if (text.Contains("engineer")) slots.CareerAmbitions = "AI / Software Engineer";
            else if (text.Contains("data scientist")) slots.CareerAmbitions = "Data Scientist";

            var missing = new System.Collections.Generic.List<string>();
            if (string.IsNullOrEmpty(slots.AcademicStage)) missing.Add("Academic Stage");
            if (slots.CoreSkills.Count == 0) missing.Add("Core Skills");
            if (slots.HobbiesInterests.Count == 0) missing.Add("Hobbies & Interests");
            if (string.IsNullOrEmpty(slots.CareerAmbitions)) missing.Add("Career Ambitions");

            bool isComplete = missing.Count == 0;
            string reply = isComplete
                ? "Wonderful! I have collected all your details and your personalized pathway is ready!"
                : $"Great! Please also tell me about your {missing[0]}.";

            return new AgentChatResponseDto
            {
                ReplyMessage = reply,
                ExtractedSlots = slots,
                MissingSlots = missing,
                IsComplete = isComplete
            };
        }
    }
}
