using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace PathwayNavigator.Api.DTOs.Pathway
{
    public class PlannerProfileDto
    {
        [JsonPropertyName("academic_stage")]
        public string? AcademicStage { get; set; }

        [JsonPropertyName("core_skills")]
        public List<string> CoreSkills { get; set; } = new();

        [JsonPropertyName("career_ambitions")]
        public string? CareerAmbitions { get; set; }
    }

    public class PathwayPlannerRequestDto
    {
        [JsonPropertyName("selected_pathway")]
        public string SelectedPathway { get; set; } = string.Empty;

        [JsonPropertyName("profile")]
        public PlannerProfileDto Profile { get; set; } = new();

        [JsonPropertyName("completed_phases")]
        public List<string> CompletedPhases { get; set; } = new();
    }

    public class RoadmapStageDto
    {
        [JsonPropertyName("order")]
        public int Order { get; set; }

        [JsonPropertyName("stage")]
        public string Stage { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("outcome")]
        public string Outcome { get; set; } = string.Empty;

        [JsonPropertyName("actions")]
        public List<string> Actions { get; set; } = new();

        [JsonPropertyName("estimated_duration")]
        public string EstimatedDuration { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;
    }

    public class PathwayPlannerResponseDto
    {
        // Populated by the backend once persisted — absent on the raw Python response.
        [JsonPropertyName("id")]
        public Guid? Id { get; set; }

        [JsonPropertyName("workflow_id")]
        public string WorkflowId { get; set; } = string.Empty;

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("selected_pathway")]
        public string SelectedPathway { get; set; } = string.Empty;

        [JsonPropertyName("roadmap")]
        public List<RoadmapStageDto> Roadmap { get; set; } = new();

        [JsonPropertyName("missing_skills")]
        public List<string> MissingSkills { get; set; } = new();

        [JsonPropertyName("completed_phases")]
        public List<string> CompletedPhases { get; set; } = new();

        [JsonPropertyName("next_action")]
        public string NextAction { get; set; } = string.Empty;

        /// <summary>
        /// Consultant guidance attached to this roadmap (stages, resources, checklists). Serialized
        /// <c>List&lt;RoadmapGuidanceItem&gt;</c>. Rendered as a "Consultant-recommended" resource on the
        /// matching milestone, so an answer arrives where the student asked rather than in a separate inbox.
        /// </summary>
        [JsonPropertyName("consultant_guidance_json")]
        public string ConsultantGuidanceJson { get; set; } = "[]";

        [JsonPropertyName("validation_errors")]
        public List<string> ValidationErrors { get; set; } = new();

        // Populated by the backend once persisted — absent on the raw Python response.
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    public class UpdateRoadmapProgressDto
    {
        [JsonPropertyName("completed_phases")]
        public List<string> CompletedPhases { get; set; } = new();
    }
}