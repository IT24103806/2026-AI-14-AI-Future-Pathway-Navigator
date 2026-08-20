using System;

namespace PathwayNavigator.Api.Models
{
    public class VerificationCode
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Purpose { get; set; } = "PasswordReset"; // e.g. "PasswordReset", "EmailVerification"
        public DateTime ExpiresAt { get; set; }
        public bool IsUsed { get; set; } = false;
        public int AttemptCount { get; set; } = 0;

        // Optional relationship to User
        public Guid? UserId { get; set; }
        public User? User { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
