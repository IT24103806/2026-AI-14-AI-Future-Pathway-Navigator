using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.DTOs.Review;

namespace PathwayNavigator.Api.Services
{
    public class AgentService : IAgentService
    {
        /// <summary>
        /// Matches explicit self-introductions only ("My name is Nimal Perera", "call me Nimal").
        /// The capital-letter requirement keeps phrases like "I am an undergraduate" from being
        /// mistaken for a name.
        /// </summary>
        private static readonly Regex NameStatementPattern = new(
            @"\b(?:my\s+name\s+is|call\s+me|i\s+am|i'm)\s+((?-i:[A-Z])[a-zA-Z'’\-]+(?:\s+(?-i:[A-Z])[a-zA-Z'’\-]+){0,3})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

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
                    current_slots = request.CurrentSlots,
                    baseline_slots = request.BaselineSlots,
                    update_mode = request.UpdateMode
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/agent-1/chat", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AgentChatResponseDto>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (result != null)
                    {
                        return ApplyUpdateModeGuard(request, result);
                    }
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

        /// <summary>
        /// Keeps "Re-run AI onboarding" meaningful regardless of how the agent answered: while the
        /// student is revising an existing profile, the turn only counts as finished once at least
        /// one detail actually differs from the stored profile. Without this, a chat that starts
        /// from the saved slots would "complete" on the first message and save nothing new.
        /// </summary>
        private static AgentChatResponseDto ApplyUpdateModeGuard(AgentChatRequestDto request, AgentChatResponseDto response)
        {
            if (!request.UpdateMode || !response.IsComplete)
            {
                return response;
            }

            if (SlotsDiffer(request.BaselineSlots, response.ExtractedSlots))
            {
                return response;
            }

            response.IsComplete = false;
            response.IsSaved = false;
            response.ReplyMessage =
                "I already have your saved profile loaded and nothing has changed yet. " +
                "Tell me what you'd like to update — your name, academic stage, skills, interests or career ambition?";
            return response;
        }

        /// <summary>True when the freshly extracted slots differ from the stored profile in any field.</summary>
        private static bool SlotsDiffer(ExtractedSlotsDto? baseline, ExtractedSlotsDto? current)
        {
            if (baseline == null || current == null)
            {
                return true;
            }

            return !SameText(baseline.FullName, current.FullName)
                || !SameText(baseline.AcademicStage, current.AcademicStage)
                || !SameText(baseline.CareerAmbitions, current.CareerAmbitions)
                || !SameSet(baseline.CoreSkills, current.CoreSkills)
                || !SameSet(baseline.HobbiesInterests, current.HobbiesInterests);
        }

        private static bool SameText(string? left, string? right) =>
            string.Equals((left ?? string.Empty).Trim(), (right ?? string.Empty).Trim(), StringComparison.OrdinalIgnoreCase);

        private static bool SameSet(List<string>? left, List<string>? right) =>
            new HashSet<string>(
                (left ?? new List<string>()).Select(value => value.Trim()),
                StringComparer.OrdinalIgnoreCase)
            .SetEquals(
                (right ?? new List<string>()).Select(value => value.Trim()));

        public async Task<PathwayAnalysisResponseDto?> ProcessAgent2AnalysisAsync(StudentProfileDto profile, string? userId = null)
        {
            try
            {
                var payload = new
                {
                    user_id = userId,
                    profile = new
                    {
                        academic_stage = profile.AcademicStage,
                        core_skills = profile.CoreSkills,
                        hobbies_interests = profile.HobbiesInterests,
                        career_ambitions = profile.CareerAmbitions
                    }
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/agent-2/analyze", payload);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<PathwayAnalysisResponseDto>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                    return result;
                }

                var errorText = await response.Content.ReadAsStringAsync();
                _logger.LogError("AI Microservice (Agent 2) returned error status {StatusCode}: {ErrorText}", response.StatusCode, errorText);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with AI microservice (Agent 2) at {BaseAddress}", _httpClient.BaseAddress);
            }

            // No fabricated fallback here — career recommendations must come from the real agent.
            return null;
        }

        public async Task<RealityCheckAgentResponseDto?> ProcessAgent4RealityCheckAsync(RealityCheckAgentRequestDto request)
        {
            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    using var response = await _httpClient.PostAsJsonAsync("/api/v1/agent-4/evaluate", request);
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogWarning("Agent 4 attempt {Attempt} returned {StatusCode}", attempt, response.StatusCode);
                        if ((int)response.StatusCode < 500) return null;
                    }
                    else
                    {
                        return await response.Content.ReadFromJsonAsync<RealityCheckAgentResponseDto>(
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }

                }
                catch (TaskCanceledException ex) { _logger.LogError(ex, "Agent 4 attempt {Attempt} timed out", attempt); }
                catch (HttpRequestException ex) { _logger.LogError(ex, "Agent 4 attempt {Attempt} failed", attempt); }
                if (attempt < 2) await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            return null;
        }

        public async Task<PathwayPlannerResponseDto?> ProcessAgent3PlanAsync(PathwayPlannerRequestDto request, string? userId = null)
        {
            try
            {
                var payload = new
                {
                    user_id = userId,
                    selected_pathway = request.SelectedPathway,
                    profile = request.Profile,
                    completed_phases = request.CompletedPhases
                };

                var response = await _httpClient.PostAsJsonAsync("/api/v1/agent-3/plan", payload);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<PathwayPlannerResponseDto>(new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }

                _logger.LogError("AI Microservice (Agent 3) returned error status {StatusCode}", response.StatusCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to communicate with AI microservice (Agent 3) at {BaseAddress}", _httpClient.BaseAddress);
            }

            return null;
        }

        private AgentChatResponseDto GenerateFallbackTurn(AgentChatRequestDto request)
        {
            var slots = request.CurrentSlots ?? new ExtractedSlotsDto();
            var text = request.Message.ToLower();

            // Name: only obvious "my name is ..." / "call me ..." statements, never a guess.
            var nameMatch = NameStatementPattern.Match(request.Message);
            if (nameMatch.Success)
            {
                slots.FullName = nameMatch.Groups[1].Value.Trim();
            }

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
            bool nothingChangedYet = request.UpdateMode && !SlotsDiffer(request.BaselineSlots, slots);

            string reply;
            if (isComplete && nothingChangedYet)
            {
                // Update session where the student has not changed anything (yet).
                isComplete = false;
                reply = "I have your saved profile loaded — tell me what you'd like to update " +
                        "(name, academic stage, skills, interests or career ambition) and I'll apply it.";
            }
            else if (isComplete)
            {
                reply = request.UpdateMode
                    ? "Thanks! I have updated your profile with the new details."
                    : "Wonderful! I have collected all your details and your personalized pathway is ready!";
            }
            else
            {
                reply = $"Great! Please also tell me about your {missing[0]}.";
            }

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
