namespace StudyCourseAPI.DTOs.Responses.User;

/// <summary>
/// GET api/User/me/overview — mọi thứ trang cá nhân cần, trừ contribution graph
/// (lấy riêng qua api/User/me/activity để dùng chung cache với Settings / Home).
/// </summary>
public class UserOverviewResponse
{
    public UserProfileResponse Profile { get; set; } = null!;
    public DateTime JoinedAt { get; set; }

    public UserStatsResponse Stats { get; set; } = null!;

    /// <summary>Điểm tích lũy: bài học 10 · quiz đạt 20 (chưa đạt 5) · chứng chỉ 100 · mỗi ngày có học 5.</summary>
    public int Points { get; set; }
    public RankResponse Rank { get; set; } = null!;

    public int CurrentStreak { get; set; }
    public int LongestStreak { get; set; }
    public bool StreakSafeToday { get; set; }

    public List<SkillProgressResponse> Skills { get; set; } = new();
    public List<CertificateResponse> Certificates { get; set; } = new();
    public List<ContinueCourseResponse> ContinueLearning { get; set; } = new();
    public List<RecentEventResponse> Recent { get; set; } = new();
    public List<AchievementResponse> Achievements { get; set; } = new();
}

public class UserStatsResponse
{
    public int EnrolledCourses { get; set; }
    public int CompletedCourses { get; set; }
    /// <summary>% khóa đã ghi danh mà hoàn thành (0 khi chưa ghi danh).</summary>
    public double CompletionRate { get; set; }
    public int LessonsCompleted { get; set; }
    /// <summary>Tổng thời lượng các bài đã hoàn thành (giây) — xấp xỉ thời gian học.</summary>
    public int StudySeconds { get; set; }
    public int QuizAttempts { get; set; }
    public int QuizPassed { get; set; }
    public double QuizPassRate { get; set; }
    /// <summary>Điểm cao nhất từng đạt (0–100).</summary>
    public double BestScore { get; set; }
    public int Certificates { get; set; }
    /// <summary>Số ngày có học trong 365 ngày.</summary>
    public int ActiveDays { get; set; }
}

public class RankResponse
{
    /// <summary>bronze | silver | gold | platinum | diamond</summary>
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
    public int MinPoints { get; set; }
    /// <summary>null khi đã ở hạng cao nhất.</summary>
    public string? NextLabel { get; set; }
    public int? NextMinPoints { get; set; }
}

public class SkillProgressResponse
{
    /// <summary>language | framework</summary>
    public string Kind { get; set; } = null!;
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? IconUrl { get; set; }
    public int CompletedLessons { get; set; }
    public int TotalLessons { get; set; }
    /// <summary>0–100</summary>
    public double Progress { get; set; }
}

public class ContinueCourseResponse
{
    public long CourseId { get; set; }
    public string Title { get; set; } = null!;
    public string? ImageUrl { get; set; }
    public string Level { get; set; } = null!;
    /// <summary>0–100 (UserCourses.Progress)</summary>
    public double Progress { get; set; }
    public int LessonCount { get; set; }
    public int CompletedLessons { get; set; }
    /// <summary>Bài chưa hoàn thành đầu tiên theo thứ tự chương/bài — đích của nút "Học tiếp".</summary>
    public long? NextLessonId { get; set; }
    public string? NextLessonTitle { get; set; }
    public DateTime? LastActivityAt { get; set; }
}

/// <summary>Kind: lesson | quiz | cert | enroll</summary>
public class RecentEventResponse
{
    public string Kind { get; set; } = null!;
    public string Title { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long? LessonId { get; set; }
    /// <summary>Điểm quiz / chứng chỉ (0–100)</summary>
    public double? Value { get; set; }
    public bool? Passed { get; set; }
    public DateTime At { get; set; }
}

public class AchievementResponse
{
    public string Id { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string Description { get; set; } = null!;
    public bool Earned { get; set; }
    /// <summary>Tiến độ hiện tại / mục tiêu — để vẽ "còn 3 bài nữa".</summary>
    public int Progress { get; set; }
    public int Target { get; set; }
}
