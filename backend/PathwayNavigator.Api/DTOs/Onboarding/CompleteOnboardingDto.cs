using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Onboarding
{
    public class CompleteOnboardingDto
    {
        [Required(ErrorMessage = "Academic stage is required.")]
        public string AcademicStage { get; set; } = string.Empty;

        public List<string> CoreSkills { get; set; } = new();

        public List<string> HobbiesInterests { get; set; } = new();

        [Required(ErrorMessage = "Career ambitions are required.")]
        public string CareerAmbitions { get; set; } = string.Empty;

        public string? AlStream { get; set; }

        public string? AlResults { get; set; }

        public string? BudgetLevel { get; set; }

        public string OnboardingMethod { get; set; } = "ConversationalAgent"; // "ConversationalAgent" or "StandardForm"
    }
}
