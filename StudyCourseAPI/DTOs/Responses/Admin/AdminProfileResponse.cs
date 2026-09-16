using StudyCourseAPI.DTOs.Responses.User;

namespace StudyCourseAPI.DTOs.Responses.Admin;

/// <summary>
/// GET api/admin/me/overview — trang cá nhân của admin: admin không học, nên thay thống kê học tập
/// bằng đóng góp quản trị (khóa/bài viết đã tạo, câu trả lời, phản hồi) + tình trạng hệ thống.
/// Đóng góp gắn với admin qua CreatedBy/UpdatedBy (email) và UserId ở bảng trả lời/phản hồi.
/// </summary>
public class AdminProfileResponse
{
    public UserProfileResponse Profile { get; set; } = null!;
    public DateTime JoinedAt { get; set; }

    public AdminContributionResponse Contribution { get; set; } = null!;
    public AdminSystemSnapshotResponse System { get; set; } = null!;

    /// <summary>Khóa học do admin này tạo (mới nhất trước), tối đa 6.</summary>
    public List<AdminOwnedCourseResponse> MyCourses { get; set; } = new();

    /// <summary>Hành động quản trị gần đây của admin này.</summary>
    public List<AdminRecentActionResponse> Recent { get; set; } = new();

    /// <summary>Hoạt động quản trị theo ngày — cùng shape với hoạt động học tập để dùng lại contribution graph.</summary>
    public UserActivityResponse Activity { get; set; } = null!;
}

public class AdminContributionResponse
{
    public int CoursesCreated { get; set; }
    public int CoursesUpdated { get; set; }
    /// <summary>Tổng bài học trong các khóa admin này tạo.</summary>
    public int LessonsInMyCourses { get; set; }
    /// <summary>Tổng học viên đã ghi danh các khóa admin này tạo.</summary>
    public int LearnersInMyCourses { get; set; }
    public int ArticlesWritten { get; set; }
    public int AnswersGiven { get; set; }
    public int ReviewRepliesGiven { get; set; }
}

public class AdminSystemSnapshotResponse
{
    public int Courses { get; set; }
    public int ActiveCourses { get; set; }
    public int Learners { get; set; }
    public int Enrollments { get; set; }
    public int CertificatesIssued { get; set; }
    /// <summary>Câu hỏi chưa ai trả lời — việc admin cần làm.</summary>
    public int PendingQuestions { get; set; }
    /// <summary>Review ≤ 2★ chưa phản hồi.</summary>
    public int PendingLowReviews { get; set; }
    /// <summary>Khóa đang mở nhưng chưa có bài kiểm tra cuối.</summary>
    public int CoursesWithoutTest { get; set; }
}

public class AdminOwnedCourseResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string Level { get; set; } = null!;
    public bool IsActive { get; set; }
    public decimal Price { get; set; }
    public int LessonCount { get; set; }
    public int Learners { get; set; }
    public double Rating { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>Kind: course_created | course_updated | article | answer | reply</summary>
public class AdminRecentActionResponse
{
    public string Kind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public long? CourseId { get; set; }
    public long? ArticleId { get; set; }
    public long? LessonId { get; set; }
    public DateTime At { get; set; }
}
