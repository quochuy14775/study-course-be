using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>Trang cá nhân của admin đang đăng nhập: đóng góp quản trị + tình trạng hệ thống.</summary>
public interface IAdminProfileService
{
    Task<AdminProfileResponse?> GetMyProfileAsync();
}
