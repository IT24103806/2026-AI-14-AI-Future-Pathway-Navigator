using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Onboarding
{
    public class AgentChatResponseDto
    {
        [JsonPropertyName("reply_message")]
        public string ReplyMessage { get; set; } = string.Empty;

        [JsonPropertyName("extracted_slots")]
        public ExtractedSlotsDto ExtractedSlots { get; set; } = new();

        [JsonPropertyName("missing_slots")]
        public List<string> MissingSlots { get; set; } = new();

        [JsonPropertyName("is_complete")]
        public bool IsComplete { get; set; } = false;

        [JsonPropertyName("is_saved")]
        public bool IsSaved { get; set; } = false;
    }
}
