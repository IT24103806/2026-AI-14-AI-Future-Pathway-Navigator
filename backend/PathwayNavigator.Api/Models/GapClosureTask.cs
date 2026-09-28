using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.Models;

public class GapClosureTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid StudentId { get; set; }
    public User Student { get; set; } = null!;
    public Guid PathwayReviewId { get; set; }
    public PathwayReview PathwayReview { get; set; } = null!;
    [Required, MaxLength(160)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(20)] public string Status { get; set; } = "ToDo";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
