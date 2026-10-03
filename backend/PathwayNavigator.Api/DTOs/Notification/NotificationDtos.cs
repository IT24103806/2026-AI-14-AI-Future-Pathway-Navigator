using System.ComponentModel.DataAnnotations;

namespace PathwayNavigator.Api.DTOs.Notification;

public class NotificationDto
{
    public Guid Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string? DeepLink { get; set; }
    public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string Priority { get; set; } = "Info";
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PagedNotificationsDto
{
    public IReadOnlyList<NotificationDto> Items { get; set; } = Array.Empty<NotificationDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class UnreadCountDto
{
    public int UnreadCount { get; set; }
}

public class CreateNotificationDto
{
    [Required] public Guid UserId { get; set; }
    [Required, MaxLength(60)] public string Type { get; set; } = string.Empty;
    [Required, MaxLength(120)] public string Title { get; set; } = string.Empty;
    [Required, MaxLength(400)] public string Body { get; set; } = string.Empty;
    [MaxLength(300)] public string? DeepLink { get; set; }
    [MaxLength(40)] public string? EntityType { get; set; }
    public Guid? EntityId { get; set; }
    [MaxLength(20)] public string Priority { get; set; } = "Info";
    /// <summary>When set, a second notification with the same key is silently skipped.</summary>
    [MaxLength(200)] public string? DedupeKey { get; set; }
}
