using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Consultant;

/// <summary>Admin-only payload used to provision a staff (Consultant) account.</summary>
public class CreateConsultantDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required, MinLength(8)]
    public string Password { get; set; } = string.Empty;

    [MaxLength(120)]
    public string? FullName { get; set; }

    [MaxLength(160)]
    public string? Headline { get; set; }

    [MaxLength(12)]
    public List<string> Expertise { get; set; } = new();

    [MaxLength(6)]
    public List<string> Languages { get; set; } = new();

    [Range(1, 200)]
    public int MaxOpenCases { get; set; } = 10;
}

public class UpdateConsultantDto
{
    [MaxLength(120)] public string? FullName { get; set; }
    [MaxLength(160)] public string? Headline { get; set; }
    [MaxLength(12)] public List<string>? Expertise { get; set; }
    [MaxLength(6)] public List<string>? Languages { get; set; }
    [Range(1, 200)] public int? MaxOpenCases { get; set; }
    public bool? IsAcceptingRequests { get; set; }
    public bool? IsActive { get; set; }
    /// <summary>Required when resetting the account password.</summary>
    [MinLength(8)] public string? NewPassword { get; set; }
}

/// <summary>Consultant self-service profile update (cannot change activation or role).</summary>
public class UpdateConsultantSelfDto
{
    [MaxLength(160)] public string? Headline { get; set; }
    [MaxLength(12)] public List<string>? Expertise { get; set; }
    [MaxLength(6)] public List<string>? Languages { get; set; }
    [Range(1, 200)] public int? MaxOpenCases { get; set; }
    public bool? IsAcceptingRequests { get; set; }
}

public class ConsultantProfileDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Headline { get; set; } = string.Empty;
    public List<string> Expertise { get; set; } = new();
    public List<string> Languages { get; set; } = new();
    public bool IsAcceptingRequests { get; set; }
    public bool IsActive { get; set; }
    public int MaxOpenCases { get; set; }
    public int OpenCaseCount { get; set; }
    public int ResolvedCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ConsultantStatsDto
{
    public int OpenCases { get; set; }
    public int AssignedToMe { get; set; }
    public int UnclaimedPool { get; set; }
    public int AwaitingStudent { get; set; }
    public int ResolvedThisWeek { get; set; }
    public double AverageFirstResponseHours { get; set; }
    public double SlaCompliancePercent { get; set; }
    public double AverageRating { get; set; }
    public int RatedCount { get; set; }
    public int DraftAssistedReplies { get; set; }
    public double DraftAcceptancePercent { get; set; }
}

public class ConversationMetricsDto
{
    public int TotalRequests { get; set; }
    public int OpenRequests { get; set; }
    public int ClosedRequests { get; set; }
    public int SlaBreaches { get; set; }
    public double AverageFirstResponseHours { get; set; }
    public double SlaCompliancePercent { get; set; }
    public double AverageRating { get; set; }
    public int FaqDeflections { get; set; }
    public Dictionary<string, int> RequestsByCategory { get; set; } = new();
    public Dictionary<string, int> RequestsByContext { get; set; } = new();
    public Dictionary<string, int> RequestsByPriority { get; set; } = new();
}
