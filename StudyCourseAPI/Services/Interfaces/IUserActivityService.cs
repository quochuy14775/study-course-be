using StudyCourseAPI.DTOs.Responses.User;

namespace StudyCourseAPI.Services;

/// <summary>
/// Hoạt động học tập theo ngày của user đang đăng nhập — nguồn cho contribution graph và streak.
/// </summary>
public interface IUserActivityService
{
    /// <param name="days">Số ngày lùi từ hôm nay (giờ VN), 7–730.</param>
    Task<UserActivityResponse> GetMyActivityAsync(int days);
}
