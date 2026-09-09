using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Services;

public interface INotificationService
{
    /// <summary>
    /// Push a notification to a single user. Skips if userId is 0 or equals actorId (no self-notify).
    /// </summary>
    Task NotifyAsync(long userId, string message, NotificationType type = NotificationType.Info, string? linkUrl = null, long? actorId = null);

    /// <summary>
    /// Broadcast a notification to ALL active users (excluding actor). Use sparingly.
    /// </summary>
    Task NotifyAllAsync(string message, NotificationType type = NotificationType.Info, string? linkUrl = null, long? actorId = null);
}
