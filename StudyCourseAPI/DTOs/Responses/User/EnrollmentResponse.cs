using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

/// <summary>
/// Trạng thái học của user trên một khóa. FE dùng để quyết định nút hiển thị
/// "Học ngay" / "Tiếp tục học" / "Xem lại".
/// </summary>
public class EnrollmentResponse
{
    public long CourseId { get; set; }
    public DateTime EnrolledAt { get; set; }

    /// <summary>0 → 100, tính từ số bài đã hoàn thành trên tổng số bài.</summary>
    public double Progress { get; set; }

    /// <summary>Chỉ true khi user đã pass course test và được cấp chứng chỉ.</summary>
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public EnrollmentResponse(UserCourse enrollment)
    {
        CourseId = enrollment.CourseId;
        EnrolledAt = enrollment.EnrolledAt;
        Progress = enrollment.Progress;
        IsCompleted = enrollment.IsCompleted;
        CompletedAt = enrollment.CompletedAt;
    }
}

/// <summary>Một dòng trong "Khóa học của tôi" — enrollment kèm đủ dữ liệu để render card.</summary>
public class EnrolledCourseResponse : EnrollmentResponse
{
    public CourseResponse Course { get; set; }

    public EnrolledCourseResponse(UserCourse enrollment) : base(enrollment)
    {
        Course = new CourseResponse(enrollment.Course);
    }
}
