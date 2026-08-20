using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Auth
{
    public class GoogleLoginRequestDto
    {
        [Required(ErrorMessage = "Google ID token is required.")]
        public string IdToken { get; set; } = string.Empty;
    }
}
