using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services
{
    public class StudentProfileService : IStudentProfileService
    {
        private readonly AppDbContext _context;

        public StudentProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileStatusDto> GetProfileStatusAsync(Guid userId)
        {
            var profile = await _context.StudentProfiles
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                return new ProfileStatusDto
                {
                    HasProfile = false,
                    IsOnboardingCompleted = false,
                    AcademicStage = null
                };
            }

            return new ProfileStatusDto
            {
                HasProfile = true,
                IsOnboardingCompleted = profile.IsOnboardingCompleted,
                AcademicStage = profile.AcademicStage
            };
        }

        public async Task<StudentProfileDto?> GetProfileByUserIdAsync(Guid userId)
        {
            var profile = await _context.StudentProfiles
                .Include(p => p.User)
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null) return null;

            return new StudentProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Email = profile.User?.Email ?? string.Empty,
                FullName = profile.User?.FullName,
                AcademicStage = profile.AcademicStage,
                CoreSkills = profile.CoreSkills,
                HobbiesInterests = profile.HobbiesInterests,
                CareerAmbitions = profile.CareerAmbitions,
                IsOnboardingCompleted = profile.IsOnboardingCompleted,
                OnboardingMethod = profile.OnboardingMethod,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        public async Task<StudentProfileDto> SaveOrUpdateProfileAsync(Guid userId, CompleteOnboardingDto dto)
        {
            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AcademicStage = dto.AcademicStage,
                    CoreSkills = dto.CoreSkills ?? new(),
                    HobbiesInterests = dto.HobbiesInterests ?? new(),
                    CareerAmbitions = dto.CareerAmbitions,
                    IsOnboardingCompleted = true,
                    OnboardingMethod = dto.OnboardingMethod,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StudentProfiles.Add(profile);
            }
            else
            {
                profile.AcademicStage = dto.AcademicStage;
                profile.CoreSkills = dto.CoreSkills ?? new();
                profile.HobbiesInterests = dto.HobbiesInterests ?? new();
                profile.CareerAmbitions = dto.CareerAmbitions;
                profile.IsOnboardingCompleted = true;
                profile.OnboardingMethod = dto.OnboardingMethod;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return new StudentProfileDto
            {
                Id = profile.Id,
                UserId = profile.UserId,
                Email = user?.Email ?? string.Empty,
                FullName = user?.FullName,
                AcademicStage = profile.AcademicStage,
                CoreSkills = profile.CoreSkills,
                HobbiesInterests = profile.HobbiesInterests,
                CareerAmbitions = profile.CareerAmbitions,
                IsOnboardingCompleted = profile.IsOnboardingCompleted,
                OnboardingMethod = profile.OnboardingMethod,
                CreatedAt = profile.CreatedAt,
                UpdatedAt = profile.UpdatedAt
            };
        }

        public async Task<bool> SaveFromExtractedSlotsAsync(Guid userId, ExtractedSlotsDto slots, string method = "ConversationalAgent")
        {
            if (string.IsNullOrWhiteSpace(slots.AcademicStage) || string.IsNullOrWhiteSpace(slots.CareerAmbitions))
            {
                return false;
            }

            var dto = new CompleteOnboardingDto
            {
                AcademicStage = slots.AcademicStage,
                CoreSkills = slots.CoreSkills,
                HobbiesInterests = slots.HobbiesInterests,
                CareerAmbitions = slots.CareerAmbitions,
                OnboardingMethod = method
            };

            await SaveOrUpdateProfileAsync(userId, dto);
            return true;
        }
    }
}
