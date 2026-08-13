using System.Threading.Tasks;
using PathwayNavigator.Api.DTOs.Auth;

namespace PathwayNavigator.Api.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto?> RegisterAsync(RegisterRequestDto request);
        Task<AuthResponseDto?> LoginAsync(LoginRequestDto request);
    }
}