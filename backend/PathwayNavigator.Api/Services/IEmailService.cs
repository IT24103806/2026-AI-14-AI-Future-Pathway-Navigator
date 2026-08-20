using System.Threading.Tasks;

namespace PathwayNavigator.Api.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string code);
    }
}
