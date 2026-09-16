using StudyCourseAPI.DTOs.Responses.User;

namespace StudyCourseAPI.Services;

/// <summary>Tổng hợp trang cá nhân của user đang đăng nhập: thống kê, kỹ năng, chứng chỉ, học tiếp, thành tích.</summary>
public interface IUserOverviewService
{
    Task<UserOverviewResponse?> GetMyOverviewAsync();
}
