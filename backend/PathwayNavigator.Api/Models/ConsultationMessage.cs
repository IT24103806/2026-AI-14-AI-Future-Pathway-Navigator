using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathwayNavigator.Api.Models
{
    /// <summary>
    /// One entry in a consultation thread. Student-visible messages and consultant-only
    /// <see cref="IsInternal"/> notes share this table so the audit trail stays complete.
    /// </summary>
    public class ConsultationMessage
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ConsultationRequestId { get; set; }

        [ForeignKey(nameof(ConsultationRequestId))]
        public ConsultationRequest? ConsultationRequest { get; set; }

        [Required]
        public Guid AuthorUserId { get; set; }

        [ForeignKey(nameof(AuthorUserId))]
        public User? Author { get; set; }

        /// <summary>Role snapshot taken when the message was written (Student | Consultant | Admin).</summary>
        [Required, MaxLength(30)]
        public string AuthorRole { get; set; } = "Student";

        [Required, MaxLength(4000)]
        public string Body { get; set; } = string.Empty;

        /// <summary>
        /// Consultant-only working note. Never returned by any student-facing endpoint
        /// (enforced in the service projection, not just in the UI).
        /// </summary>
        public bool IsInternal { get; set; }

        /// <summary>Serialized <c>List&lt;ResourceLink&gt;</c>: [{ label, url, kind }]. Links only - no uploads.</summary>
        public string ResourcesJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? EditedAt { get; set; }

        /// <summary>Soft delete keeps the audit trail intact instead of removing history.</summary>
        public DateTime? DeletedAt { get; set; }
    }
}
