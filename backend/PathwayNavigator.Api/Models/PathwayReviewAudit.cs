using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.Models;

public class PathwayReviewAudit
{
    [Key] public Guid Id { get; set; } = Guid.NewGuid();
    [Required] public Guid PathwayReviewId { get; set; }
    public PathwayReview? PathwayReview { get; set; }
    public Guid? ActorUserId { get; set; }
    [Required, MaxLength(60)] public string Action { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string FromStatus { get; set; } = string.Empty;
    [Required, MaxLength(50)] public string ToStatus { get; set; } = string.Empty;
    [MaxLength(1000)] public string? Details { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
