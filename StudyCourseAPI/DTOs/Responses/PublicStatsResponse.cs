namespace StudyCourseAPI.DTOs.Responses;

/// <summary>
/// GET api/stats/public — số liệu "social proof" cho trang chủ, không cần đăng nhập.
/// Cache 5 phút ở server vì mọi lượt vào trang chủ đều gọi.
/// </summary>
public class PublicStatsResponse
{
    public int Learners { get; set; }
    public int ActiveCourses { get; set; }
    public int Lessons { get; set; }
    public int Enrollments { get; set; }
    public int CertificatesIssued { get; set; }
    /// <summary>Rating trung bình của các khóa có review (0–5, 1 chữ số thập phân); 0 khi chưa có review.</summary>
    public double AverageRating { get; set; }
    /// <summary>% enrollment đã hoàn thành (0–100, 1 chữ số thập phân).</summary>
    public double CompletionRate { get; set; }
    /// <summary>Thời điểm tính (UTC) — để FE biết số đang cache.</summary>
    public DateTime ComputedAt { get; set; }
}
