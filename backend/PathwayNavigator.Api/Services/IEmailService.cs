using System.Threading.Tasks;

namespace PathwayNavigator.Api.Services
{
    public interface IEmailService
    {
        Task SendPasswordResetCodeAsync(string toEmail, string code);

        /// <summary>
        /// Optional out-of-band delivery for consultation events the student should not miss
        /// (a consultant replied, a Reality Check decision was recorded). Implementations must
        /// never throw: the in-app notification is already committed by the time this is called.
        /// </summary>
        Task SendConsultationUpdateAsync(string toEmail, string subject, string message);
    }
}
