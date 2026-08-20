using System;

namespace PathwayNavigator.Api.Models
{
    public class User
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Email { get; set; } = string.Empty;
        public string? PasswordHash { get; set; }
        public bool IsActive { get; set; } = true;
        
        // OAuth / Profile fields
        public string? GoogleId { get; set; }
        public string? AuthProvider { get; set; } // "Local", "Google", etc.
        public string? FullName { get; set; }
        public string? ProfilePictureUrl { get; set; }

        // Foreign Key
        public Guid RoleId { get; set; }
        public Role? Role { get; set; }

        // Audit Fields[cite: 2]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}