using PathwayNavigator.Api.DTOs.Notification;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Persistence + read model for the per-user notification inbox.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Writes one notification. Never throws: a notification failure must not roll back the
    /// business action that caused it (see <see cref="DbNotificationPublisher"/>).
    /// </summary>
    Task PublishAsync(CreateNotificationDto notification);

    /// <summary>Publishes to every active user holding the given role (e.g. the consultant pool).</summary>
    Task PublishToRoleAsync(string roleName, CreateNotificationDto notification);

    Task<PagedNotificationsDto> GetForUserAsync(Guid userId, bool unreadOnly, int page, int pageSize);

    Task<int> GetUnreadCountAsync(Guid userId);

    Task<bool> MarkReadAsync(Guid userId, Guid notificationId);

    Task<int> MarkAllReadAsync(Guid userId);
}
