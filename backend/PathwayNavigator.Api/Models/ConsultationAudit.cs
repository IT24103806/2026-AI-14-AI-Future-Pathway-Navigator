using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathwayNavigator.Api.Models
{
    /// <summary>
    /// Append-only history of a consultation's state transitions and staff actions.
    /// Deliberately mirrors <see cref="PathwayReviewAudit"/> so both human-in-the-loop
    /// workflows can be explained the same way during review/demo.
    /// </summary>
    public class ConsultationAudit
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid ConsultationRequestId { get; set; }

        [ForeignKey(nameof(ConsultationRequestId))]
        public ConsultationRequest? ConsultationRequest { get; set; }

        public Guid? ActorUserId { get; set; }

        [ForeignKey(nameof(ActorUserId))]
        public User? Actor { get; set; }

        /// <summary>Created | Claimed | Reassigned | Replied | InternalNoteAdded | Escalated | Resolved | Closed | Reopened | AutoExpired | PriorityChanged | Rated | Viewed</summary>
        [Required, MaxLength(60)]
        public string Action { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string FromStatus { get; set; } = string.Empty;

        [Required, MaxLength(30)]
        public string ToStatus { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Details { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
