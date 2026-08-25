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
        Task<bool> SaveFromExtractedSlotsAsync(Guid userId, ExtractedSlotsDto slots, string method = "ConversationalAgent");
    }
}
