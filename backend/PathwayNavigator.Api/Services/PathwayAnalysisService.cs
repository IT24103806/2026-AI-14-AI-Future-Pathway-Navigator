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
    public class PathwayAnalysisService : IPathwayAnalysisService
    {
        private readonly AppDbContext _context;

        public PathwayAnalysisService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<PathwayAnalysisResponseDto> CreateAsync(Guid studentProfileId, PathwayAnalysisResponseDto agentResult)
        {
            var entity = new PathwayAnalysis
            {
                StudentProfileId = studentProfileId,
                WorkflowId = agentResult.WorkflowId,
                RecommendationsJson = JsonSerializer.Serialize(agentResult.Recommendations),
                Status = agentResult.Status,
                CreatedAt = DateTime.UtcNow
            };

            _context.PathwayAnalyses.Add(entity);
            await _context.SaveChangesAsync();

            agentResult.Id = entity.Id;
            agentResult.CreatedAt = entity.CreatedAt;
            return agentResult;
        }

        public async Task<PathwayAnalysisResponseDto?> GetByIdForUserAsync(Guid id, Guid userId)
        {
            var entity = await _context.PathwayAnalyses
                .AsNoTracking()
                .Include(a => a.StudentProfile)
                .FirstOrDefaultAsync(a => a.Id == id && a.StudentProfile!.UserId == userId);

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<PathwayAnalysisResponseDto?> GetLatestForUserAsync(Guid userId)
        {
            var entity = await _context.PathwayAnalyses
                .AsNoTracking()
                .Include(a => a.StudentProfile)
                .Where(a => a.StudentProfile!.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .FirstOrDefaultAsync();

            return entity == null ? null : MapToDto(entity);
        }

        public async Task<IReadOnlyList<PathwayAnalysisResponseDto>> GetHistoryForUserAsync(Guid userId)
        {
            var entities = await _context.PathwayAnalyses
                .AsNoTracking()
                .Include(a => a.StudentProfile)
                .Where(a => a.StudentProfile!.UserId == userId)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return entities.Select(MapToDto).ToList();
        }

        public async Task<PathwayAnalysisResponseDto?> SetStatusAsync(Guid id, Guid userId, string newStatus)
        {
            var entity = await _context.PathwayAnalyses
                .Include(a => a.StudentProfile)
                .FirstOrDefaultAsync(a => a.Id == id && a.StudentProfile!.UserId == userId);

            if (entity == null) return null;

            if (entity.Status != "pending_approval")
            {
                throw new InvalidOperationException(
                    $"Pathway analysis '{id}' is already '{entity.Status}' and cannot be changed.");
            }

            entity.Status = newStatus;
            entity.ApprovedAt = DateTime.UtcNow;
            entity.ApprovedByUserId = userId;
            await _context.SaveChangesAsync();

            return MapToDto(entity);
        }

        private static PathwayAnalysisResponseDto MapToDto(PathwayAnalysis entity)
        {
            var recommendations = JsonSerializer.Deserialize<List<CareerPathRecommendationDto>>(entity.RecommendationsJson)
                ?? new List<CareerPathRecommendationDto>();

            return new PathwayAnalysisResponseDto
            {
                Id = entity.Id,
                WorkflowId = entity.WorkflowId,
                Status = entity.Status,
                Recommendations = recommendations,
                CreatedAt = entity.CreatedAt
            };
        }
    }
}
