using System;
using System.Collections.Generic;

namespace PathwayNavigator.Api.Models
{
    public class StudentProfile
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        // 1-to-1 Relationship with User
        public Guid UserId { get; set; }
        public User? User { get; set; }

        public string AcademicStage { get; set; } = string.Empty; // "After O/L", "After A/L", "Undergraduate", "Graduated"
        public List<string> CoreSkills { get; set; } = new();
        public List<string> HobbiesInterests { get; set; } = new();
        public string CareerAmbitions { get; set; } = string.Empty;

        // Saved Reality Check academic & financial context for pre-filling and revisions
        public string AlStream { get; set; } = string.Empty;
        public string AlResults { get; set; } = string.Empty;
        public string BudgetLevel { get; set; } = "Medium";

        public bool IsOnboardingCompleted { get; set; } = false;
        public string OnboardingMethod { get; set; } = "ConversationalAgent"; // "ConversationalAgent" or "StandardForm"

        // Audit Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
