using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>
/// Certificate được cấp như side effect của việc pass course test (xem QuizService),
/// không bao giờ tạo tay. Service này chỉ đọc và (cho admin) thu hồi.
/// </summary>
public interface ICertificateService
{
    /// <summary>Certificate của chính user đang đăng nhập cho 1 course.</summary>
    Task<CertificateResponse?> GetOwnByCourseAsync(long courseId);

    /// <summary>Admin: danh sách toàn bộ, lọc theo course và tìm theo tên/email/mã.</summary>
    Task<List<CertificateAdminResponse>> SearchAsync(long? courseId, string? search);

    /// <summary>Public: tra cứu theo mã xác thực.</summary>
    Task<CertificateAdminResponse?> VerifyAsync(string code);

    /// <summary>Admin: thu hồi certificate cấp nhầm (hard delete — entity không có cờ soft-delete).</summary>
    Task<bool> RevokeAsync(long id);
}
