using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Auth
{
    public class RegisterRequestDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string RoleName { get; set; } = "Student"; // Default role
    }
}