using System;

namespace PathwayNavigator.Api.Models
{
    public class PathwayPlan
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StudentProfileId { get; set; }
        public StudentProfile? StudentProfile { get; set; }

        public string WorkflowId { get; set; } = string.Empty;

        public string SelectedPathway { get; set; } = string.Empty;

        public string Status { get; set; } = "ready"; // "ready" | "failed"

        // Serialized JSON of List<RoadmapStageDto> returned by Agent 3.
        public string RoadmapJson { get; set; } = "[]";

        // Serialized JSON of List<string>
        public string MissingSkillsJson { get; set; } = "[]";

        // Serialized JSON of List<string> (completed stage identifiers)
        public string CompletedPhasesJson { get; set; } = "[]";

        public string NextAction { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
