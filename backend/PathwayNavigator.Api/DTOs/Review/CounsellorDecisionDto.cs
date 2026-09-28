namespace PathwayNavigator.Api.DTOs.Review;

public class CounsellorDecisionDto
{
    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.RegularExpression("^(Approved|Rejected|NeedsRevision)$")]
    public string Decision { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Required]
    [System.ComponentModel.DataAnnotations.StringLength(1000, MinimumLength = 5)]
    public string Feedback { get; set; } = string.Empty;
}
