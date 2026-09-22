namespace PathwayNavigator.Api.DTOs.Review;

public class CounsellorDecisionDto
{
    // Approved, Rejected, NeedsRevision
    public string Decision { get; set; } = string.Empty;
    public string Feedback { get; set; } = string.Empty;
}