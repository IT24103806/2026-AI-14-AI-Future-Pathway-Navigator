using System;

namespace PathwayNavigator.Api.Models
{
    public class PathwayAnalysis
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public Guid StudentProfileId { get; set; }
        public StudentProfile? StudentProfile { get; set; }

        public string WorkflowId { get; set; } = string.Empty;

        // Serialized JSON of the List<CareerPathRecommendationDto> returned by Agent 2.
        public string RecommendationsJson { get; set; } = string.Empty;

        public string Status { get; set; } = "pending_approval"; // "pending_approval" | "approved" | "rejected"

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        public Guid? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }
    }
}
