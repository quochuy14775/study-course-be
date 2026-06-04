using StudyCourseAPI.Enums;

namespace StudyCourseAPI.DTOs.Responses.User;

public class NotificationResponse
{
    public long Id { get; set; }
    public string Message { get; set; } = null!;
    public NotificationType Type { get; set; }
    public string? LinkUrl { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}
