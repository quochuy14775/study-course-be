using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _repo;
    private readonly IRepository<ApplicationUser> _userRepo;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IRepository<Notification> repo,
        IRepository<ApplicationUser> userRepo,
        ILogger<NotificationService> logger)
    {
        _repo = repo;
        _userRepo = userRepo;
        _logger = logger;
    }

    public async Task NotifyAsync(long userId, string message, NotificationType type = NotificationType.Info, string? linkUrl = null, long? actorId = null)
    {
        if (userId <= 0) return;
        if (actorId.HasValue && actorId.Value == userId) return;

        try
        {
            await _repo.AddAsync(BuildNotification(userId, message, type, linkUrl));
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create notification for user {UserId}", userId);
        }
    }

    public async Task NotifyAllAsync(string message, NotificationType type = NotificationType.Info, string? linkUrl = null, long? actorId = null)
    {
        try
        {
            // Fetch only IDs (no tracking, projection) — minimal data transfer.
            var userIds = await _userRepo.Query()
                .AsNoTracking()
                .Where(u => u.IsActive && !u.IsDeleted)
                .Where(u => actorId == null || u.Id != actorId.Value)
                .Select(u => u.Id)
                .ToListAsync();

            if (userIds.Count == 0) return;

            // Build all in-memory, then AddRange + single SaveChanges.
            // ChangeTracker.AutoDetectChangesEnabled toggle would help even more, but Repository hides DbContext.
            var notifications = userIds.ConvertAll(uid => BuildNotification(uid, message, type, linkUrl));
            _repo.AddRange(notifications);
            await _repo.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to broadcast notification");
        }
    }

    private static Notification BuildNotification(long userId, string message, NotificationType type, string? linkUrl)
        => new()
        {
            UserId    = userId,
            Message   = message,
            Type      = type,
            LinkUrl   = linkUrl,
            IsRead    = false,
            IsActive  = true,
            CreatedAt = DateTime.UtcNow,
        };
}
