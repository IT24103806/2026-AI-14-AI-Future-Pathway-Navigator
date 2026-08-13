using System;
using System.Collections.Generic;

namespace PathwayNavigator.Api.Models
{
    public class Role
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = string.Empty;
        
        // Audit Fields[cite: 2]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property for EF Core
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}