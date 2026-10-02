using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Onboarding
{
    public class ChatMessageDto
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "user"; // "user", "assistant", "system"

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class ExtractedSlotsDto
    {
        [JsonPropertyName("full_name")]
        public string? FullName { get; set; }

        [JsonPropertyName("academic_stage")]
        public string? AcademicStage { get; set; }

        [JsonPropertyName("core_skills")]
        public List<string> CoreSkills { get; set; } = new();

        [JsonPropertyName("hobbies_interests")]
        public List<string> HobbiesInterests { get; set; } = new();

        [JsonPropertyName("career_ambitions")]
        public string? CareerAmbitions { get; set; }
    }

    public class AgentChatRequestDto
    {
        [Required(ErrorMessage = "Message text is required.")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("history")]
        public List<ChatMessageDto> History { get; set; } = new();

        [JsonPropertyName("current_slots")]
        public ExtractedSlotsDto? CurrentSlots { get; set; }

        /// <summary>
        /// Profile as it is stored right now. Sent while the student is revisiting an existing
        /// profile so the agent can tell a real change from "nothing was updated yet".
        /// </summary>
        [JsonPropertyName("baseline_slots")]
        public ExtractedSlotsDto? BaselineSlots { get; set; }

        /// <summary>True for a profile update session ("Re-run AI onboarding"), false for first-time onboarding.</summary>
        [JsonPropertyName("update_mode")]
        public bool UpdateMode { get; set; }
    }
}
