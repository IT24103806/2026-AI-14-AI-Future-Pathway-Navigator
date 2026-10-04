using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Consultant;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

public class ConsultantAccountService : IConsultantAccountService
{
    private readonly AppDbContext _context;
    private readonly ILogger<ConsultantAccountService> _logger;

    public ConsultantAccountService(AppDbContext context, ILogger<ConsultantAccountService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ConsultantProfileDto>> GetAllAsync()
    {
        var consultants = await _context.Users.AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.ConsultantProfile)
            .Where(u => u.Role!.Name == "Consultant")
            .OrderBy(u => u.Email)
            .ToListAsync();

        var openCounts = await _context.ConsultationRequests.AsNoTracking()
            .Where(r => r.AssignedConsultantId != null && r.ClosedAt == null)
            .GroupBy(r => r.AssignedConsultantId!.Value)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConsultantId, x => x.Count);

        var resolvedCounts = await _context.ConsultationRequests.AsNoTracking()
            .Where(r => r.AssignedConsultantId != null && r.ClosedAt != null)
            .GroupBy(r => r.AssignedConsultantId!.Value)
            .Select(g => new { ConsultantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ConsultantId, x => x.Count);

        return consultants.Select(u => Map(
            u,
            openCounts.TryGetValue(u.Id, out var open) ? open : 0,
            resolvedCounts.TryGetValue(u.Id, out var resolved) ? resolved : 0)).ToList();
    }

    public async Task<ConsultantProfileDto> CreateAsync(CreateConsultantDto input)
    {
        var email = input.Email.ToLower().Trim();
        if (await _context.Users.AnyAsync(u => u.Email == email))
        {
            throw new InvalidOperationException("An account with this email address already exists.");
        }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Consultant")
                   ?? throw new InvalidOperationException("The 'Consultant' role is missing from the database. Run the latest migrations.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.Password),
            FullName = string.IsNullOrWhiteSpace(input.FullName) ? null : input.FullName.Trim(),
            AuthProvider = "Local",
            RoleId = role.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.ConsultantProfile = new ConsultantProfile
        {
            UserId = user.Id,
            Headline = input.Headline?.Trim() ?? string.Empty,
            ExpertiseJson = JsonSerializer.Serialize(Clean(input.Expertise)),
            LanguagesJson = JsonSerializer.Serialize(Clean(input.Languages)),
            MaxOpenCases = Math.Clamp(input.MaxOpenCases, 1, 200),
            IsAcceptingRequests = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Consultant account provisioned for {Email}", email);

        return Map(user, 0, 0);
    }

    public async Task<ConsultantProfileDto?> UpdateAsync(Guid userId, UpdateConsultantDto input)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .Include(u => u.ConsultantProfile)
            .SingleOrDefaultAsync(u => u.Id == userId);
        if (user == null || user.Role?.Name != "Consultant")
        {
            return null;
        }

        if (input.FullName != null)
        {
            user.FullName = string.IsNullOrWhiteSpace(input.FullName) ? null : input.FullName.Trim();
        }

        if (input.IsActive.HasValue)
        {
            user.IsActive = input.IsActive.Value;
        }

        if (!string.IsNullOrWhiteSpace(input.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(input.NewPassword);
            user.AuthProvider = "Local";
        }

        user.UpdatedAt = DateTime.UtcNow;

        var profile = user.ConsultantProfile;
        if (profile == null)
        {
            profile = new ConsultantProfile { UserId = user.Id };
            _context.ConsultantProfiles.Add(profile);
        }

        if (input.Headline != null) profile.Headline = input.Headline.Trim();
        if (input.Expertise != null) profile.ExpertiseJson = JsonSerializer.Serialize(Clean(input.Expertise));
        if (input.Languages != null) profile.LanguagesJson = JsonSerializer.Serialize(Clean(input.Languages));
        if (input.MaxOpenCases.HasValue) profile.MaxOpenCases = Math.Clamp(input.MaxOpenCases.Value, 1, 200);
        if (input.IsAcceptingRequests.HasValue) profile.IsAcceptingRequests = input.IsAcceptingRequests.Value;
        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var open = await _context.ConsultationRequests.AsNoTracking()
            .CountAsync(r => r.AssignedConsultantId == userId && r.ClosedAt == null);
        var resolved = await _context.ConsultationRequests.AsNoTracking()
            .CountAsync(r => r.AssignedConsultantId == userId && r.ClosedAt != null);

        return Map(user, open, resolved);
    }

    public async Task<ConversationMetricsDto> GetMetricsAsync()
    {
        var requests = await _context.ConsultationRequests.AsNoTracking().ToListAsync();

        var closed = requests.Where(r => r.ClosedAt != null).ToList();
        var responded = requests.Where(r => r.FirstRespondedAt != null).ToList();
        var rated = requests.Where(r => r.StudentRating != null).Select(r => r.StudentRating!.Value).ToList();
        var withinSla = responded.Count(r => r.FirstRespondedAt!.Value <= r.SlaDueAt);

        return new ConversationMetricsDto
        {
            TotalRequests = requests.Count,
            OpenRequests = requests.Count(r => r.ClosedAt == null),
            ClosedRequests = closed.Count,
            SlaBreaches = responded.Count(r => r.FirstRespondedAt!.Value > r.SlaDueAt),
            AverageFirstResponseHours = responded.Count == 0
                ? 0
                : Math.Round(responded.Average(r => (r.FirstRespondedAt!.Value - r.CreatedAt).TotalHours), 2),
            SlaCompliancePercent = responded.Count == 0
                ? 100
                : Math.Round(withinSla * 100d / responded.Count, 1),
            AverageRating = rated.Count == 0 ? 0 : Math.Round(rated.Average(), 2),
            RequestsByCategory = requests.GroupBy(r => r.Category).ToDictionary(g => g.Key, g => g.Count()),
            RequestsByContext = requests.GroupBy(r => r.ContextType).ToDictionary(g => g.Key, g => g.Count()),
            RequestsByPriority = requests.GroupBy(r => r.Priority).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private static List<string> Clean(IEnumerable<string>? values) =>
        (values ?? Enumerable.Empty<string>())
        .Select(v => (v ?? string.Empty).Trim())
        .Where(v => v.Length is > 0 and <= 80)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Take(12)
        .ToList();

    private static ConsultantProfileDto Map(User user, int openCases, int resolved) => new()
    {
        UserId = user.Id,
        Email = user.Email,
        FullName = user.FullName ?? string.Empty,
        Headline = user.ConsultantProfile?.Headline ?? string.Empty,
        Expertise = ParseList(user.ConsultantProfile?.ExpertiseJson),
        Languages = ParseList(user.ConsultantProfile?.LanguagesJson),
        IsAcceptingRequests = user.ConsultantProfile?.IsAcceptingRequests ?? true,
        IsActive = user.IsActive,
        MaxOpenCases = user.ConsultantProfile?.MaxOpenCases ?? 10,
        OpenCaseCount = openCases,
        ResolvedCount = resolved,
        CreatedAt = user.ConsultantProfile?.CreatedAt ?? user.CreatedAt
    };

    private static List<string> ParseList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<string>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
