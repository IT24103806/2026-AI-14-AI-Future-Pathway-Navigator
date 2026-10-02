using System;
using System.Collections.Generic;
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

            return profile == null ? null : MapToDto(profile, profile.User);
        }

        public async Task<StudentProfileDto> SaveOrUpdateProfileAsync(Guid userId, CompleteOnboardingDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User account was not found.");

            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);

            if (profile == null)
            {
                profile = new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AcademicStage = dto.AcademicStage?.Trim() ?? string.Empty,
                    CoreSkills = NormalizeList(dto.CoreSkills),
                    HobbiesInterests = NormalizeList(dto.HobbiesInterests),
                    CareerAmbitions = dto.CareerAmbitions?.Trim() ?? string.Empty,
                    AlStream = dto.AlStream?.Trim() ?? string.Empty,
                    AlResults = dto.AlResults?.Trim().ToUpperInvariant() ?? string.Empty,
                    BudgetLevel = NormalizeBudget(dto.BudgetLevel),
                    IsOnboardingCompleted = true,
                    OnboardingMethod = dto.OnboardingMethod,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.StudentProfiles.Add(profile);
            }
            else
            {
                profile.AcademicStage = dto.AcademicStage?.Trim() ?? profile.AcademicStage;
                profile.CoreSkills = NormalizeList(dto.CoreSkills);
                profile.HobbiesInterests = NormalizeList(dto.HobbiesInterests);
                profile.CareerAmbitions = dto.CareerAmbitions?.Trim() ?? profile.CareerAmbitions;
                if (!string.IsNullOrWhiteSpace(dto.AlStream))
                {
                    profile.AlStream = dto.AlStream.Trim();
                }
                if (!string.IsNullOrWhiteSpace(dto.AlResults))
                {
                    profile.AlResults = dto.AlResults.Trim().ToUpperInvariant();
                }
                if (!string.IsNullOrWhiteSpace(dto.BudgetLevel))
                {
                    profile.BudgetLevel = dto.BudgetLevel.Trim();
                }
                profile.IsOnboardingCompleted = true;
                profile.OnboardingMethod = dto.OnboardingMethod;
                profile.UpdatedAt = DateTime.UtcNow;
            }

            ApplyFullName(user, dto.FullName);

            await _context.SaveChangesAsync();
            return MapToDto(profile, user);
        }

        public async Task<StudentProfileDto> UpdateProfileAsync(Guid userId, UpdateStudentProfileDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId)
                ?? throw new KeyNotFoundException("User account was not found.");

            var profile = await _context.StudentProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            var isNewProfile = profile == null;

            if (profile == null)
            {
                // A student can edit their details before the onboarding profile exists: create the
                // row so nothing is lost, and let the completeness check below decide the flag.
                profile = new StudentProfile
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    IsOnboardingCompleted = false,
                    OnboardingMethod = "ManualUpdate"
                };
            }

            ApplyFullName(user, dto.FullName);

            if (!string.IsNullOrWhiteSpace(dto.AcademicStage))
            {
                profile.AcademicStage = dto.AcademicStage.Trim();
            }

            if (!string.IsNullOrWhiteSpace(dto.CareerAmbitions))
            {
                profile.CareerAmbitions = dto.CareerAmbitions.Trim();
            }

            // Null keeps the stored list; an explicit (possibly empty) list replaces it.
            if (dto.CoreSkills != null)
            {
                profile.CoreSkills = NormalizeList(dto.CoreSkills);
            }

            if (dto.HobbiesInterests != null)
            {
                profile.HobbiesInterests = NormalizeList(dto.HobbiesInterests);
            }

            if (dto.AlStream != null)
            {
                profile.AlStream = dto.AlStream.Trim();
            }

            if (dto.AlResults != null)
            {
                profile.AlResults = dto.AlResults.Trim().ToUpperInvariant();
            }

            if (!string.IsNullOrWhiteSpace(dto.BudgetLevel))
            {
                profile.BudgetLevel = dto.BudgetLevel.Trim();
            }

            // Once every essential detail is present the profile counts as onboarded again.
            if (HasEssentialDetails(profile))
            {
                profile.IsOnboardingCompleted = true;
            }

            profile.UpdatedAt = DateTime.UtcNow;
            if (isNewProfile)
            {
                _context.StudentProfiles.Add(profile);
            }

            await _context.SaveChangesAsync();
            return MapToDto(profile, user);
        }

        public async Task<bool> SaveFromExtractedSlotsAsync(Guid userId, ExtractedSlotsDto slots, string method = "ConversationalAgent")
        {
            if (string.IsNullOrWhiteSpace(slots.AcademicStage) || string.IsNullOrWhiteSpace(slots.CareerAmbitions))
            {
                return false;
            }

            var dto = new CompleteOnboardingDto
            {
                FullName = slots.FullName,
                AcademicStage = slots.AcademicStage,
                CoreSkills = slots.CoreSkills,
                HobbiesInterests = slots.HobbiesInterests,
                CareerAmbitions = slots.CareerAmbitions,
                OnboardingMethod = method
            };

            await SaveOrUpdateProfileAsync(userId, dto);
            return true;
        }

        // ---------------------------------------------------------------- helpers

        private static void ApplyFullName(User user, string? fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
            {
                return;
            }

            user.FullName = fullName.Trim();
            user.UpdatedAt = DateTime.UtcNow;
        }

        private static bool HasEssentialDetails(StudentProfile profile) =>
            !string.IsNullOrWhiteSpace(profile.AcademicStage) &&
            !string.IsNullOrWhiteSpace(profile.CareerAmbitions) &&
            profile.CoreSkills.Count > 0 &&
            profile.HobbiesInterests.Count > 0;

        /// <summary>Trims entries, drops blanks and removes case-insensitive duplicates.</summary>
        private static List<string> NormalizeList(IEnumerable<string>? values) =>
            (values ?? Enumerable.Empty<string>())
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

        private static string NormalizeBudget(string? budgetLevel) =>
            string.IsNullOrWhiteSpace(budgetLevel) ? "Medium" : budgetLevel.Trim();

        private static StudentProfileDto MapToDto(StudentProfile profile, User? user) => new()
        {
            Id = profile.Id,
            UserId = profile.UserId,
            Email = user?.Email ?? string.Empty,
            FullName = user?.FullName,
            AcademicStage = profile.AcademicStage,
            CoreSkills = profile.CoreSkills,
            HobbiesInterests = profile.HobbiesInterests,
            CareerAmbitions = profile.CareerAmbitions,
            AlStream = profile.AlStream,
            AlResults = profile.AlResults,
            BudgetLevel = NormalizeBudget(profile.BudgetLevel),
            IsOnboardingCompleted = profile.IsOnboardingCompleted,
            OnboardingMethod = profile.OnboardingMethod,
            CreatedAt = profile.CreatedAt,
            UpdatedAt = profile.UpdatedAt
        };
    }
}
