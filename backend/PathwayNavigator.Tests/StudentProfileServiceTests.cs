using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Onboarding;
using PathwayNavigator.Api.DTOs.Profile;
using PathwayNavigator.Api.Models;
using PathwayNavigator.Api.Services;
using Xunit;

namespace PathwayNavigator.Tests
{
    /// <summary>
    /// Covers the "Re-run AI onboarding" / profile editing paths: a partial update must change only
    /// what the student sent, and must reach the Users table for the display name.
    /// </summary>
    public class StudentProfileServiceTests
    {
        private static AppDbContext NewContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        private static (AppDbContext context, StudentProfileService service, Guid userId) SeedProfile(
            string fullName = "Nimal Perera",
            string academicStage = "Undergraduate",
            string careerAmbitions = "AI Engineer")
        {
            var context = NewContext();
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = $"{Guid.NewGuid()}@test.com",
                FullName = fullName,
                RoleId = Guid.NewGuid()
            };
            context.Users.Add(user);
            context.StudentProfiles.Add(new StudentProfile
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                AcademicStage = academicStage,
                CoreSkills = new List<string> { "Python", "SQL" },
                HobbiesInterests = new List<string> { "AI" },
                CareerAmbitions = careerAmbitions,
                AlStream = "Physical Science",
                AlResults = "3B",
                BudgetLevel = "Medium",
                IsOnboardingCompleted = true,
                OnboardingMethod = "StandardForm"
            });
            context.SaveChanges();

            return (context, new StudentProfileService(context), user.Id);
        }

        [Fact]
        public async Task UpdateProfileAsync_AppliesOnlyTheFieldsThatWereSent()
        {
            var (context, service, userId) = SeedProfile();

            var result = await service.UpdateProfileAsync(userId, new UpdateStudentProfileDto
            {
                CareerAmbitions = "Data Scientist"
            });

            Assert.Equal("Data Scientist", result.CareerAmbitions);
            // Everything else stays exactly as it was.
            Assert.Equal("Undergraduate", result.AcademicStage);
            Assert.Equal(new List<string> { "Python", "SQL" }, result.CoreSkills);
            Assert.Equal(new List<string> { "AI" }, result.HobbiesInterests);
            Assert.Equal("Physical Science", result.AlStream);
            Assert.Equal("Medium", result.BudgetLevel);

            var stored = await context.StudentProfiles.SingleAsync(p => p.UserId == userId);
            Assert.Equal("Data Scientist", stored.CareerAmbitions);
        }

        [Fact]
        public async Task UpdateProfileAsync_WritesTheDisplayNameToTheUserAccount()
        {
            var (context, service, userId) = SeedProfile();

            var result = await service.UpdateProfileAsync(userId, new UpdateStudentProfileDto
            {
                FullName = "  Nimal Jayasuriya  "
            });

            Assert.Equal("Nimal Jayasuriya", result.FullName);
            var user = await context.Users.SingleAsync(u => u.Id == userId);
            Assert.Equal("Nimal Jayasuriya", user.FullName);
        }

        [Fact]
        public async Task UpdateProfileAsync_ReplacesSkillAndInterestListsIncludingDuplicates()
        {
            var (context, service, userId) = SeedProfile();

            var result = await service.UpdateProfileAsync(userId, new UpdateStudentProfileDto
            {
                CoreSkills = new List<string> { " Python ", "Docker", "docker", "" },
                HobbiesInterests = new List<string> { "Robotics" }
            });

            Assert.Equal(new List<string> { "Python", "Docker" }, result.CoreSkills);
            Assert.Equal(new List<string> { "Robotics" }, result.HobbiesInterests);
            Assert.NotNull(await context.StudentProfiles.SingleOrDefaultAsync(p => p.UserId == userId));
        }

        [Fact]
        public async Task UpdateProfileAsync_CreatesAProfileWhenTheStudentDoesNotHaveOneYet()
        {
            var context = NewContext();
            var user = new User { Id = Guid.NewGuid(), Email = "fresh@test.com", RoleId = Guid.NewGuid() };
            context.Users.Add(user);
            context.SaveChanges();
            var service = new StudentProfileService(context);

            var result = await service.UpdateProfileAsync(user.Id, new UpdateStudentProfileDto
            {
                FullName = "Amaya Silva",
                AcademicStage = "After A/L",
                CoreSkills = new List<string> { "Mathematics" },
                HobbiesInterests = new List<string> { "Chess" },
                CareerAmbitions = "Software Engineer"
            });

            Assert.Equal("Amaya Silva", result.FullName);
            Assert.Equal("After A/L", result.AcademicStage);
            // All four essentials are present, so the profile counts as onboarded.
            Assert.True(result.IsOnboardingCompleted);
        }

        [Fact]
        public async Task UpdateProfileAsync_KeepsProfileIncompleteWhileDetailsAreMissing()
        {
            var context = NewContext();
            var user = new User { Id = Guid.NewGuid(), Email = "partial@test.com", RoleId = Guid.NewGuid() };
            context.Users.Add(user);
            context.SaveChanges();
            var service = new StudentProfileService(context);

            var result = await service.UpdateProfileAsync(user.Id, new UpdateStudentProfileDto
            {
                CareerAmbitions = "Game Developer"
            });

            Assert.False(result.IsOnboardingCompleted);
        }

        [Fact]
        public async Task UpdateProfileAsync_ThrowsWhenTheUserDoesNotExist()
        {
            var context = NewContext();
            var service = new StudentProfileService(context);

            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.UpdateProfileAsync(Guid.NewGuid(), new UpdateStudentProfileDto { FullName = "Nobody" }));
        }

        [Fact]
        public async Task SaveFromExtractedSlotsAsync_PersistsTheNameCollectedByTheAgent()
        {
            var (context, service, userId) = SeedProfile(fullName: "Nimal Perera");

            var saved = await service.SaveFromExtractedSlotsAsync(userId, new ExtractedSlotsDto
            {
                FullName = "Nimal Jayasuriya",
                AcademicStage = "Graduated",
                CoreSkills = new List<string> { "Python", "Docker" },
                HobbiesInterests = new List<string> { "AI", "Robotics" },
                CareerAmbitions = "ML Engineer"
            });

            Assert.True(saved);
            var result = await service.GetProfileByUserIdAsync(userId);
            Assert.NotNull(result);
            Assert.Equal("Nimal Jayasuriya", result!.FullName);
            Assert.Equal("Graduated", result.AcademicStage);
            Assert.Contains("Docker", result.CoreSkills);
            Assert.Equal("ML Engineer", result.CareerAmbitions);
        }

        [Fact]
        public async Task SaveFromExtractedSlotsAsync_RefusesIncompleteData()
        {
            var (_, service, userId) = SeedProfile();

            var saved = await service.SaveFromExtractedSlotsAsync(userId, new ExtractedSlotsDto
            {
                AcademicStage = "Undergraduate"
            });

            Assert.False(saved);
        }

        [Fact]
        public async Task GetProfileByUserIdAsync_ReturnsNullWhenThereIsNoProfile()
        {
            var context = NewContext();
            var service = new StudentProfileService(context);

            Assert.Null(await service.GetProfileByUserIdAsync(Guid.NewGuid()));
        }
    }
}
