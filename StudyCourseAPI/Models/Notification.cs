using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Models;

/// <summary>
/// In-app notification shown in the header dropdown.
/// e.g. "Khóa học mới: Advanced TypeScript", "Bạn đã hoàn thành React Fundamentals".
/// </summary>
public class Notification : BaseEntity<long>, IAuditable
{
    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; } = NotificationType.Info;

    /// <summary>Optional deeplink target inside the app (e.g. "/courses/42").</summary>
    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
