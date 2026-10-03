using Microsoft.EntityFrameworkCore;
using PathwayNavigator.Api.Data;
using PathwayNavigator.Api.DTOs.Notification;
using PathwayNavigator.Api.Models;

namespace PathwayNavigator.Api.Services;

/// <summary>
/// Database-backed notification inbox: the single notification spine for the whole application.
///
/// Delivery is transport-agnostic on purpose. Clients poll <c>GET /api/notifications/unread-count</c>
/// today; because every notification is a committed row with a deep link, an SSE/SignalR/FCM transport
/// can be layered on later without changing any service that publishes an event.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(AppDbContext context, ILogger<NotificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task PublishAsync(CreateNotificationDto notification)
    {
        if (notification.UserId == Guid.Empty || string.IsNullOrWhiteSpace(notification.Title))
        {
            return;
        }

        try
        {
            // Dedupe: a recurring condition (SLA breach sweeps every few minutes) must not spam the
            // inbox. The unique index on DedupeKey is the backstop; this check avoids the exception
            // in the common case.
            if (!string.IsNullOrWhiteSpace(notification.DedupeKey))
            {
                var exists = await _context.Notifications
                    .AsNoTracking()
                    .AnyAsync(n => n.DedupeKey == notification.DedupeKey);
                if (exists)
                {
                    return;
                }
            }

            _context.Notifications.Add(new Notification
            {
                UserId = notification.UserId,
                Type = notification.Type,
                Title = notification.Title,
                Body = notification.Body,
                DeepLink = notification.DeepLink,
                EntityType = notification.EntityType,
                EntityId = notification.EntityId,
                Priority = notification.Priority,
                DedupeKey = notification.DedupeKey,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            // Almost always the unique DedupeKey index losing a race - a duplicate is not an error.
            _logger.LogWarning(ex, "Notification {Type} for user {UserId} was not persisted (likely a dedupe race).",
                notification.Type, notification.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification {Type} for user {UserId} failed.", notification.Type, notification.UserId);
        }
    }

    public async Task PublishToRoleAsync(string roleName, CreateNotificationDto notification)
    {
        var recipients = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Role != null && u.Role.Name == roleName)
            .Select(u => u.Id)
            .ToListAsync();

        foreach (var userId in recipients)
        {
            var copy = new CreateNotificationDto
            {
                UserId = userId,
                Type = notification.Type,
                Title = notification.Title,
                Body = notification.Body,
                DeepLink = notification.DeepLink,
                EntityType = notification.EntityType,
                EntityId = notification.EntityId,
                Priority = notification.Priority,
                // The dedupe key is scoped per recipient so two consultants both get the alert.
                DedupeKey = notification.DedupeKey == null ? null : $"{notification.DedupeKey}:{userId}"
            };
            await PublishAsync(copy);
        }
    }

    public async Task<PagedNotificationsDto> GetForUserAsync(Guid userId, bool unreadOnly, int page, int pageSize)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
        var unreadCount = await _context.Notifications.AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);

        if (unreadOnly)
        {
            query = query.Where(n => !n.IsRead);
        }

        var total = await query.CountAsync();
        var rows = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedNotificationsDto
        {
            Items = rows.Select(Map).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total,
            UnreadCount = unreadCount
        };
    }

    public Task<int> GetUnreadCountAsync(Guid userId) =>
        _context.Notifications.AsNoTracking().CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<bool> MarkReadAsync(Guid userId, Guid notificationId)
    {
        var row = await _context.Notifications
            .SingleOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);
        if (row == null)
        {
            return false;
        }

        if (!row.IsRead)
        {
            row.IsRead = true;
            row.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<int> MarkAllReadAsync(Guid userId)
    {
        var rows = await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();
        if (rows.Count == 0)
        {
            return 0;
        }

        var now = DateTime.UtcNow;
        foreach (var row in rows)
        {
            row.IsRead = true;
            row.ReadAt = now;
        }

        await _context.SaveChangesAsync();
        return rows.Count;
    }

    private static NotificationDto Map(Notification n) => new()
    {
        Id = n.Id,
        Type = n.Type,
        Title = n.Title,
        Body = n.Body,
        DeepLink = n.DeepLink,
        EntityType = n.EntityType,
        EntityId = n.EntityId,
        Priority = n.Priority,
        IsRead = n.IsRead,
        ReadAt = n.ReadAt,
        CreatedAt = n.CreatedAt
    };
}
