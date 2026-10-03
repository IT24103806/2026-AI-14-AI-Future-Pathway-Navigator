using System;
using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.Models
{
    /// <summary>
    /// A per-user inbox entry. This is the spine that lets a student *see* that a consultant
    /// answered without polling the consultation itself.
    ///
    /// <see cref="DedupeKey"/> carries a unique index so a recurring event (e.g. an SLA breach
    /// evaluated by the background sweeper) can never create a second notification for the same
    /// condition. Delivery is deliberately transport-agnostic: the row is committed in the same
    /// transaction as the state change, and <c>INotificationPublisher</c> decides how it reaches
    /// the client (polling today, SSE/SignalR/FCM later).
    /// </summary>
    public class Notification
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        /// <summary>ConsultationSubmitted | ConsultationClaimed | ConsultationReplied | MoreInfoNeeded | Escalated | Closed | SlaBreach | AutoExpired | RealityCheckDecision</summary>
        [Required, MaxLength(60)]
        public string Type { get; set; } = string.Empty;

        [Required, MaxLength(120)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(400)]
        public string Body { get; set; } = string.Empty;

        /// <summary>In-app route the client should open, e.g. "/student/reality-check?consultation={id}".</summary>
        [MaxLength(300)]
        public string? DeepLink { get; set; }

        /// <summary>Consultation | PathwayReview | PathwayPlan</summary>
        [MaxLength(40)]
        public string? EntityType { get; set; }

        public Guid? EntityId { get; set; }

        /// <summary>Info | Success | Warning | Action</summary>
        [Required, MaxLength(20)]
        public string Priority { get; set; } = "Info";

        public bool IsRead { get; set; }
        public DateTime? ReadAt { get; set; }

        /// <summary>Optional unique key that suppresses duplicate notifications for the same event.</summary>
        [MaxLength(200)]
        public string? DedupeKey { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
