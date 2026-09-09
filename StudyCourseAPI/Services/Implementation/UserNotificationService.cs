using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class UserNotificationService : IUserNotificationService
{
    private readonly IRepository<Notification> _notificationRepository;
    private readonly ICurrentUser _currentUser;

    public UserNotificationService(
        IRepository<Notification> notificationRepository,
        ICurrentUser currentUser)
    {
        _notificationRepository = notificationRepository;
        _currentUser = currentUser;
    }

    public Task<List<NotificationResponse>> GetAllAsync(int? top, bool? unreadOnly)
    {
        var userId = _currentUser.GetCurrentUserId();

        var query = _notificationRepository.Query()
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (unreadOnly == true)
            query = query.Where(n => !n.IsRead);

        // Projection ngay trong query → chỉ lấy cột cần, không tốn change-tracking
        return query
            .OrderByDescending(n => n.CreatedAt)
            .Take(top ?? 20)
            .Select(n => new NotificationResponse
            {
                Id        = n.Id,
                Message   = n.Message,
                Type      = n.Type,
                LinkUrl   = n.LinkUrl,
                IsRead    = n.IsRead,
                CreatedAt = n.CreatedAt,
            })
            .ToListAsync();
    }

    public Task<int> GetUnreadCountAsync()
    {
        var userId = _currentUser.GetCurrentUserId();

        return _notificationRepository.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
    }

    public async Task<bool?> MarkReadAsync(long id)
    {
        var userId = _currentUser.GetCurrentUserId();
        var now = DateTime.UtcNow;

        // Atomic update, không load entity
        var affected = await _notificationRepository.Query()
            .Where(n => n.Id == id && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        if (affected > 0) return true;

        // Không update được: hoặc đã đọc rồi (false), hoặc không tồn tại (null)
        var exists = await _notificationRepository.Query()
            .AsNoTracking()
            .AnyAsync(n => n.Id == id && n.UserId == userId);

        return exists ? false : null;
    }

    public Task<int> MarkAllReadAsync()
    {
        var userId = _currentUser.GetCurrentUserId();
        var now = DateTime.UtcNow;

        return _notificationRepository.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));
    }

    public async Task<bool> DeleteAsync(long id)
    {
        var userId = _currentUser.GetCurrentUserId();

        // ExecuteDelete = 1 câu SQL, không load entity
        var affected = await _notificationRepository.Query()
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync();

        return affected > 0;
    }

    public Task<int> ClearAllAsync()
    {
        var userId = _currentUser.GetCurrentUserId();

        return _notificationRepository.Query()
            .Where(n => n.UserId == userId)
            .ExecuteDeleteAsync();
    }
}
