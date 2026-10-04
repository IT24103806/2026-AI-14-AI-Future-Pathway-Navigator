using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PathwayNavigator.Api.Models
{
    /// <summary>
    /// Extra profile data for a user holding the <c>Consultant</c> role.
    ///
    /// Kept in a separate table rather than on <see cref="User"/> so that the student/counsellor/admin
    /// flows are untouched and a consultant can be provisioned or deactivated without side effects on
    /// authentication. One-to-one with <see cref="User"/>.
    /// </summary>
    public class ConsultantProfile
    {
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        public Guid UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }

        /// <summary>Short public-facing title, e.g. "Senior Software Engineer · AI/ML mentor".</summary>
        [MaxLength(160)]
        public string Headline { get; set; } = string.Empty;

        /// <summary>Serialized <c>List&lt;string&gt;</c> used for queue routing (e.g. "AI/ML", "Scholarships").</summary>
        public string ExpertiseJson { get; set; } = "[]";

        /// <summary>Serialized <c>List&lt;string&gt;</c> of languages the consultant can answer in.</summary>
        public string LanguagesJson { get; set; } = "[]";

        /// <summary>When false the consultant keeps existing assignments but receives no new pool cases.</summary>
        public bool IsAcceptingRequests { get; set; } = true;

        /// <summary>Soft capacity limit used by the queue's "available" indicator.</summary>
        [Range(1, 200)]
        public int MaxOpenCases { get; set; } = 10;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
