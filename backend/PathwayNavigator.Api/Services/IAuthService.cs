using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Auth;

namespace PathwayNavigator.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
        Task<AuthResponseDto?> GoogleLoginAsync(GoogleLoginRequestDto request);
        Task<(bool Success, string Message)> ForgotPasswordAsync(ForgotPasswordRequestDto request);
        Task<(bool Success, string Message)> VerifyResetCodeAsync(VerifyCodeRequestDto request);
        Task<(bool Success, string Message)> ResetPasswordAsync(ResetPasswordRequestDto request);
    }
}