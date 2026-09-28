using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Review;

public class GapClosureTaskDto
{
    [Required, StringLength(160, MinimumLength = 1)] public string Title { get; set; } = string.Empty;
    [Required] public string Status { get; set; } = "ToDo";
    public DateTime? DueDate { get; set; }
}

public class CreateGapClosureTaskDto : GapClosureTaskDto
{
    [Required] public Guid PathwayReviewId { get; set; }
}
