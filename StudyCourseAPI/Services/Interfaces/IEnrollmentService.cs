using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>
/// Ghi danh học viên vào khóa học. Enrollment là bản ghi "user X đang học course Y",
/// độc lập với thanh toán — FE quyết định khi nào gọi (miễn phí thì gọi ngay, có phí thì
/// gọi sau khi checkout thành công).
/// </summary>
public interface IEnrollmentService
{
    /// <summary>Đăng ký khóa học cho user hiện tại. Idempotent — gọi lại trả về enrollment cũ.</summary>
    Task<ServiceResult<EnrollmentResponse>> EnrollAsync(long courseId);

    /// <summary>Enrollment của user hiện tại trên một khóa. Null nếu chưa đăng ký.</summary>
    Task<EnrollmentResponse?> GetOwnByCourseAsync(long courseId);

    /// <summary>Danh sách khóa đã đăng ký kèm tiến độ, mới học gần nhất lên đầu.</summary>
    Task<List<EnrolledCourseResponse>> GetMyCoursesAsync();
}
