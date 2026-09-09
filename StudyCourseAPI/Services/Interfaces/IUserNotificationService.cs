using StudyCourseAPI.DTOs.Responses.User;

namespace StudyCourseAPI.Services;

/// <summary>
/// Hộp thông báo của user đang đăng nhập (đọc / đánh dấu đã đọc / xoá).
/// Khác với <see cref="INotificationService"/> — cái đó là để hệ thống *tạo* thông báo.
/// </summary>
public interface IUserNotificationService
{
    Task<List<NotificationResponse>> GetAllAsync(int? top, bool? unreadOnly);

    Task<int> GetUnreadCountAsync();

    /// <summary>
    /// Đánh dấu 1 thông báo đã đọc. Null = không tìm thấy; false = đã đọc từ trước
    /// (cả hai trường hợp "đã tồn tại" đều trả 204 ở controller).
    /// </summary>
    Task<bool?> MarkReadAsync(long id);

    /// <summary>Đánh dấu tất cả đã đọc. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> MarkAllReadAsync();

    /// <summary>Xoá hẳn 1 thông báo của user. False nếu không tìm thấy.</summary>
    Task<bool> DeleteAsync(long id);

    /// <summary>Xoá hết thông báo của user. Trả về số bản ghi đã xoá.</summary>
    Task<int> ClearAllAsync();
}
