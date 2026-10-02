using System;
using System.Collections.Generic;

namespace PathwayNavigator.Api.DTOs.Profile
{
    public class StudentProfileDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string AcademicStage { get; set; } = string.Empty;
        public List<string> CoreSkills { get; set; } = new();
        public List<string> HobbiesInterests { get; set; } = new();
        public string CareerAmbitions { get; set; } = string.Empty;
        public string AlStream { get; set; } = string.Empty;
        public string AlResults { get; set; } = string.Empty;
        public string BudgetLevel { get; set; } = "Medium";
        public bool IsOnboardingCompleted { get; set; }
        public string OnboardingMethod { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
