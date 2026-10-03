using PathwayNavigator.Api.DTOs.Notification;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// The seam between "something happened" and "how the user finds out".
///
/// Today the only implementation writes a database row that clients poll, which is all this
/// deployment needs (no websocket infrastructure on a 949 MiB VPS). Adding SSE, SignalR or FCM later
/// means adding a decorator here - no change to <see cref="IConsultationService"/> or the queue service.
/// </summary>
public interface INotificationPublisher
{
    Task PublishAsync(CreateNotificationDto notification);
    Task PublishToRoleAsync(string roleName, CreateNotificationDto notification);
}

/// <summary>Default publisher: persists every event to the per-user inbox.</summary>
public class DbNotificationPublisher : INotificationPublisher
{
    private readonly INotificationService _notifications;

    public DbNotificationPublisher(INotificationService notifications) => _notifications = notifications;

    public Task PublishAsync(CreateNotificationDto notification) => _notifications.PublishAsync(notification);

    public Task PublishToRoleAsync(string roleName, CreateNotificationDto notification) =>
        _notifications.PublishToRoleAsync(roleName, notification);
}
