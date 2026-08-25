using System;

namespace PathwayNavigator.Api.DTOs.Profile
{
    public class ProfileStatusDto
    {
        public bool HasProfile { get; set; }
        public bool IsOnboardingCompleted { get; set; }
        public string? AcademicStage { get; set; }
    }
}
