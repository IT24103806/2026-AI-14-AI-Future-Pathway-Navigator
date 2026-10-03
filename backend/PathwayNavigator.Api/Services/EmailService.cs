using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace PathwayNavigator.Api.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendPasswordResetCodeAsync(string toEmail, string code)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(emailSettings["SmtpPort"], out var port) ? port : 587;
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var senderName = emailSettings["SenderName"] ?? "Pathway Navigator";
            var enableSsl = bool.TryParse(emailSettings["EnableSsl"], out var ssl) ? ssl : true;

            // In development or when credentials are not configured, log the OTP code to terminal
            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogInformation("\n======================================================\n[EMAIL SERVICE - DEV MODE]\nTo: {ToEmail}\nSubject: Password Reset Verification Code\nVerification Code: {Code}\n======================================================\n", toEmail, code);
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = "Pathway Navigator - Password Reset Code",
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                            <h2 style='color: #4F46E5; text-align: center;'>Pathway Navigator</h2>
                            <h3 style='color: #333;'>Password Reset Request</h3>
                            <p>You recently requested to reset your password. Use the verification code below to proceed with resetting your password:</p>
                            <div style='text-align: center; margin: 30px 0;'>
                                <span style='font-size: 32px; font-weight: bold; letter-spacing: 6px; color: #4F46E5; background: #EEF2FF; padding: 12px 24px; border-radius: 6px; display: inline-block;'>{code}</span>
                            </div>
                            <p style='color: #666; font-size: 14px;'>This code is valid for <strong>15 minutes</strong>. If you did not request this password reset, please ignore this email.</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                            <p style='color: #999; font-size: 12px; text-align: center;'>© {DateTime.UtcNow.Year} Pathway Navigator. All rights reserved.</p>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Password reset verification email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {ToEmail}. Fallback code: {Code}", toEmail, code);
                // Keep the flow operational even if SMTP provider fails temporarily
            }
        }

        /// <summary>
        /// Consultation / decision notification. Same dev-mode behaviour as the reset code: when SMTP
        /// credentials are absent the message is logged to the terminal instead of being sent, so the
        /// feature stays demonstrable without an SMTP provider.
        /// </summary>
        public async Task SendConsultationUpdateAsync(string toEmail, string subject, string message)
        {
            var emailSettings = _configuration.GetSection("EmailSettings");
            var smtpHost = emailSettings["SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.TryParse(emailSettings["SmtpPort"], out var port) ? port : 587;
            var senderEmail = emailSettings["SenderEmail"];
            var senderPassword = emailSettings["SenderPassword"];
            var senderName = emailSettings["SenderName"] ?? "Pathway Navigator";
            var enableSsl = bool.TryParse(emailSettings["EnableSsl"], out var ssl) ? ssl : true;

            var safeSubject = string.IsNullOrWhiteSpace(subject) ? "Pathway Navigator update" : subject;
            var safeMessage = System.Net.WebUtility.HtmlEncode(message);

            if (string.IsNullOrWhiteSpace(senderEmail) || string.IsNullOrWhiteSpace(senderPassword))
            {
                _logger.LogInformation(
                    "\n======================================================\n[EMAIL SERVICE - DEV MODE]\nTo: {ToEmail}\nSubject: {Subject}\nMessage: {Message}\n======================================================\n",
                    toEmail, safeSubject, safeMessage);
                return;
            }

            try
            {
                using var client = new SmtpClient(smtpHost, smtpPort)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = enableSsl
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = safeSubject,
                    Body = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e0e0e0; border-radius: 8px;'>
                            <h2 style='color: #4F46E5; text-align: center;'>Pathway Navigator</h2>
                            <p style='color: #333; font-size: 15px; line-height: 1.6;'>{safeMessage}</p>
                            <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                            <p style='color: #999; font-size: 12px; text-align: center;'>Open the app to see the full conversation and continue your pathway.</p>
                            <p style='color: #999; font-size: 12px; text-align: center;'>© {DateTime.UtcNow.Year} Pathway Navigator. All rights reserved.</p>
                        </div>",
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Consultation update email sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send consultation update email to {ToEmail}", toEmail);
            }
        }
    }
}
