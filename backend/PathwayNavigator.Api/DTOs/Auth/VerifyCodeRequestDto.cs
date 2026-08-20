using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Auth
{
    public class VerifyCodeRequestDto
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email format.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Verification code is required.")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Verification code must be exactly 6 digits.")]
        public string Code { get; set; } = string.Empty;
    }
}
