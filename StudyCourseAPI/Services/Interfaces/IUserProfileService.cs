using StudyCourseAPI.DTOs.Requests.User;
using StudyCourseAPI.DTOs.Responses.User;

namespace StudyCourseAPI.Services;

/// <summary>
/// Hồ sơ của chính user đang đăng nhập. Tất cả method trả null khi không xác định
/// được user (token hỏng / user đã bị xoá) → controller trả 401.
/// </summary>
public interface IUserProfileService
{
    Task<UserProfileResponse?> GetMeAsync();

    Task<ServiceResult<UserProfileResponse>?> UpdateProfileAsync(UpdateProfileRequest request);

    Task<ServiceResult<bool>?> ChangePasswordAsync(ChangePasswordRequest request);
}
