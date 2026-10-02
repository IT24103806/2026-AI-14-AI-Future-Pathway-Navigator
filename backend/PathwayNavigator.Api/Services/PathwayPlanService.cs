using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Pathway;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services
{
    public class PathwayPlanService : IPathwayPlanService
    {
        private readonly AppDbContext _context;

        public PathwayPlanService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PathwayPlannerResponseDto> SaveOrUpdateAsync(
            Guid studentProfileId,
            PathwayPlannerResponseDto agentResult,
            List<string>? completedPhases = null)
        {
            var normalizedPathway = (agentResult.SelectedPathway ?? string.Empty).Trim();
            var lowerPathway = normalizedPathway.ToLower();

            var effectiveCompleted = (agentResult.CompletedPhases?.Count > 0
                    ? agentResult.CompletedPhases
                    : completedPhases ?? new List<string>())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim().ToLowerInvariant())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            var completedSet = new HashSet<string>(effectiveCompleted, StringComparer.OrdinalIgnoreCase);
            foreach (var stage in agentResult.Roadmap)
            {
                stage.Status = completedSet.Contains(stage.Stage) ? "completed" : "not_started";
            }

            var canonicalCompleted = agentResult.Roadmap
                .Where(s => string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Stage)
                .ToList();

            var nextStage = agentResult.Roadmap.FirstOrDefault(
                s => !string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase));
            var nextAction = nextStage != null && nextStage.Actions.Count > 0
                ? nextStage.Actions[0]
                : agentResult.Roadmap.Count > 0
                    ? $"All roadmap milestones for {normalizedPathway} are completed! Keep your portfolio and skills updated."
                    : agentResult.NextAction;

            var existing = await _context.PathwayPlans
                .FirstOrDefaultAsync(p => p.StudentProfileId == studentProfileId && p.SelectedPathway.ToLower() == lowerPathway);

            var now = DateTime.UtcNow;

            if (existing == null)
            {
                existing = new PathwayPlan
                {
                    Id = Guid.NewGuid(),
                    StudentProfileId = studentProfileId,
                    WorkflowId = agentResult.WorkflowId,
                    SelectedPathway = normalizedPathway,
                    Status = agentResult.Status,
                    RoadmapJson = JsonSerializer.Serialize(agentResult.Roadmap),
                    MissingSkillsJson = JsonSerializer.Serialize(agentResult.MissingSkills),
                    CompletedPhasesJson = JsonSerializer.Serialize(canonicalCompleted),
                    NextAction = nextAction,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                _context.PathwayPlans.Add(existing);
            }
            else
            {
                existing.WorkflowId = agentResult.WorkflowId;
                existing.SelectedPathway = normalizedPathway;
                existing.Status = agentResult.Status;
                existing.RoadmapJson = JsonSerializer.Serialize(agentResult.Roadmap);
                existing.MissingSkillsJson = JsonSerializer.Serialize(agentResult.MissingSkills);
                existing.CompletedPhasesJson = JsonSerializer.Serialize(canonicalCompleted);
                existing.NextAction = nextAction;
                existing.UpdatedAt = now;
            }

            await _context.SaveChangesAsync();
            return MapToDto(existing);
        }

        public async Task<PathwayPlannerResponseDto?> GetLatestForUserAsync(Guid userId)
        {
            var entity = await _context.PathwayPlans
                .AsNoTracking()
                .Include(p => p.StudentProfile)
                .Where(p => p.StudentProfile!.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<PathwayPlannerResponseDto?> GetByPathwayForUserAsync(Guid userId, string selectedPathway)
        {
            if (string.IsNullOrWhiteSpace(selectedPathway)) return null;
            var lower = selectedPathway.Trim().ToLower();

            var entity = await _context.PathwayPlans
                .AsNoTracking()
                .Include(p => p.StudentProfile)
                .Where(p => p.StudentProfile!.UserId == userId && p.SelectedPathway.ToLower() == lower)
                .OrderByDescending(p => p.UpdatedAt)
                .FirstOrDefaultAsync();

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<PathwayPlannerResponseDto>> GetAllForUserAsync(Guid userId)
        {
            var entities = await _context.PathwayPlans
                .AsNoTracking()
                .Include(p => p.StudentProfile)
                .Where(p => p.StudentProfile!.UserId == userId)
                .OrderByDescending(p => p.UpdatedAt)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        public async Task<PathwayPlannerResponseDto?> UpdateProgressAsync(
            Guid planId,
            Guid userId,
            List<string> completedPhases)
        {
            var entity = await _context.PathwayPlans
                .Include(p => p.StudentProfile)
                .FirstOrDefaultAsync(p => p.Id == planId && p.StudentProfile!.UserId == userId);

            if (entity == null) return null;

            var roadmap = JsonSerializer.Deserialize<List<RoadmapStageDto>>(entity.RoadmapJson)
                ?? new List<RoadmapStageDto>();

            var completedSet = new HashSet<string>(
                (completedPhases ?? new List<string>())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim()),
                StringComparer.OrdinalIgnoreCase);

            foreach (var stage in roadmap)
            {
                stage.Status = completedSet.Contains(stage.Stage) ? "completed" : "not_started";
            }

            var canonicalCompleted = roadmap
                .Where(s => string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase))
                .Select(s => s.Stage)
                .ToList();

            var nextStage = roadmap.FirstOrDefault(
                s => !string.Equals(s.Status, "completed", StringComparison.OrdinalIgnoreCase));
            entity.NextAction = nextStage != null && nextStage.Actions.Count > 0
                ? nextStage.Actions[0]
                : $"All roadmap milestones for {entity.SelectedPathway} are completed! Keep your portfolio and skills updated.";

            entity.RoadmapJson = JsonSerializer.Serialize(roadmap);
            entity.CompletedPhasesJson = JsonSerializer.Serialize(canonicalCompleted);
            entity.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return MapToDto(entity);
        }

        private static PathwayPlannerResponseDto MapToDto(PathwayPlan entity)
        {
            var roadmap = JsonSerializer.Deserialize<List<RoadmapStageDto>>(entity.RoadmapJson)
                ?? new List<RoadmapStageDto>();
            var missingSkills = JsonSerializer.Deserialize<List<string>>(entity.MissingSkillsJson)
                ?? new List<string>();
            var completedPhases = JsonSerializer.Deserialize<List<string>>(entity.CompletedPhasesJson)
                ?? new List<string>();

            return new PathwayPlannerResponseDto
            {
                Id = entity.Id,
                WorkflowId = entity.WorkflowId,
                Status = entity.Status,
                SelectedPathway = entity.SelectedPathway,
                Roadmap = roadmap,
                MissingSkills = missingSkills,
                CompletedPhases = completedPhases,
                NextAction = entity.NextAction,
                UpdatedAt = entity.UpdatedAt
            };
        }
    }
}
