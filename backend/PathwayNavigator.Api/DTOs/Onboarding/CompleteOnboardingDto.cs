using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Onboarding
{
    public class CompleteOnboardingDto
    {
        /// <summary>
        /// Optional display name. Stored on the user account (Users.FullName) and shown on the
        /// dashboard; asked on the first run and editable afterwards.
        /// </summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string? FullName { get; set; }

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
