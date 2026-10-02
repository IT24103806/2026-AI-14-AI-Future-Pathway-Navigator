using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Profile
{
    /// <summary>
    /// Partial update of a student's details ("Re-run AI onboarding" / profile editing).
    ///
    /// Every property is optional: a null value means "leave the stored value alone", which lets the
    /// SPA send only what the student actually changed. Sending an empty list for
    /// <see cref="CoreSkills"/> or <see cref="HobbiesInterests"/> clears that list on purpose.
    /// </summary>
    public class UpdateStudentProfileDto
    {
        /// <summary>Display name stored on the user account (Users.FullName).</summary>
        [StringLength(200, MinimumLength = 2, ErrorMessage = "Full name must be between 2 and 200 characters.")]
        public string? FullName { get; set; }

        [StringLength(100, ErrorMessage = "Academic stage must be at most 100 characters.")]
        public string? AcademicStage { get; set; }

        public List<string>? CoreSkills { get; set; }

        public List<string>? HobbiesInterests { get; set; }

        [StringLength(500, ErrorMessage = "Career ambitions must be at most 500 characters.")]
        public string? CareerAmbitions { get; set; }

        [StringLength(50, ErrorMessage = "A/L stream must be at most 50 characters.")]
        public string? AlStream { get; set; }

        [StringLength(50, ErrorMessage = "A/L results must be at most 50 characters.")]
        public string? AlResults { get; set; }

        [StringLength(50, ErrorMessage = "Budget level must be at most 50 characters.")]
        public string? BudgetLevel { get; set; }
    }
}
