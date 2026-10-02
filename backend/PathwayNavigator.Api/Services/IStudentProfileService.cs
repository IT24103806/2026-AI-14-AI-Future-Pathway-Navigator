using System;
using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.DTOs.Profile;

namespace PathwayNavigator.Api.Services
{
    public interface IStudentProfileService
    {
        Task<ProfileStatusDto> GetProfileStatusAsync(Guid userId);
        Task<StudentProfileDto?> GetProfileByUserIdAsync(Guid userId);
        Task<StudentProfileDto> SaveOrUpdateProfileAsync(Guid userId, CompleteOnboardingDto dto);

        /// <summary>
        /// Partial update used by profile editing / "Re-run AI onboarding": only the fields present
        /// in <paramref name="dto"/> are applied. Also updates the user's display name.
        /// </summary>
        Task<StudentProfileDto> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto dto);

        Task<bool> SaveFromExtractedSlotsAsync(Guid userId, ExtractedSlotsDto slots, string method = "ConversationalAgent");
    }
}
