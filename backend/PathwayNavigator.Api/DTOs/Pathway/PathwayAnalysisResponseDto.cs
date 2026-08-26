using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Pathway
{
    public class ExecutionTraceEntryDto
    {
        [JsonPropertyName("node")]
        public string Node { get; set; } = string.Empty;

        [JsonPropertyName("duration_ms")]
        public double DurationMs { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "success" | "error"
    }

    public class RoadmapStepDto
    {
        [JsonPropertyName("phase")]
        public string Phase { get; set; } = string.Empty; // "Foundation", "Core Skills", "Specialization"

        [JsonPropertyName("course")]
        public string Course { get; set; } = string.Empty;
    }

    public class CareerPathRecommendationDto
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty; // "Path A", "Path B", "Path C"

        [JsonPropertyName("pathway_name")]
        public string PathwayName { get; set; } = string.Empty;

        [JsonPropertyName("match_score")]
        public int MatchScore { get; set; }

        [JsonPropertyName("demand_score")]
        public int DemandScore { get; set; }

        [JsonPropertyName("competition_score")]
        public int CompetitionScore { get; set; }

        [JsonPropertyName("trend")]
        public string Trend { get; set; } = string.Empty; // "rising" | "stable" | "declining"

        [JsonPropertyName("data_source")]
        public string DataSource { get; set; } = string.Empty; // "adzuna_live" | "simulated_fallback"

        [JsonPropertyName("reasoning")]
        public string Reasoning { get; set; } = string.Empty;

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("recommended_courses")]
        public List<string> RecommendedCourses { get; set; } = new();

        [JsonPropertyName("roadmap")]
        public List<RoadmapStepDto> Roadmap { get; set; } = new();
    }

    public class PathwayAnalysisResponseDto
    {
        // Populated by the backend once persisted — absent on the raw Python response.
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty; // "pending_approval" | "approved" | "rejected" | "failed"

        [JsonPropertyName("recommendations")]
        public List<CareerPathRecommendationDto> Recommendations { get; set; } = new();

        [JsonPropertyName("validation_errors")]
        public List<string> ValidationErrors { get; set; } = new();

        [JsonPropertyName("execution_trace")]
        public List<ExecutionTraceEntryDto> ExecutionTrace { get; set; } = new();
    }
}
