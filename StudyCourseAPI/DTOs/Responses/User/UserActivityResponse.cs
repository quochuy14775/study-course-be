namespace StudyCourseAPI.DTOs.Responses.User;

/// <summary>
/// GET api/User/me/activity?days=365 — hoạt động học tập theo ngày (contribution graph) + streak.
/// Ngày tính theo múi giờ Việt Nam. Ngày không có hoạt động vẫn có mặt với Count = 0.
///
/// Nguồn: bài học hoàn thành, lượt làm quiz, ghi chú / bình luận / hỏi đáp, ghi danh, chứng chỉ.
/// Chưa tính thời gian xem video (LastWatchedAt chỉ lưu lần cuối, không dựng lại lịch sử được) —
/// cần bảng UserDailyActivities + heartbeat để streak phản ánh cả "xem mà chưa xong bài".
/// </summary>
public class UserActivityResponse
{
    /// <summary>yyyy-MM-dd của hôm nay theo giờ VN — FE dùng làm mốc thay vì giờ máy client.</summary>
    public string Today { get; set; } = null!;

    /// <summary>Tăng dần theo ngày, phần tử cuối là hôm nay.</summary>
    public List<UserActivityDayResponse> Days { get; set; } = new();

    /// <summary>Số ngày liên tiếp có hoạt động, kết thúc ở hôm nay (hoặc hôm qua nếu hôm nay chưa học).</summary>
    public int CurrentStreak { get; set; }

    /// <summary>Chuỗi dài nhất trong khoảng truy vấn.</summary>
    public int LongestStreak { get; set; }

    /// <summary>Hôm nay đã có hoạt động → streak an toàn. False + CurrentStreak &gt; 0 = "còn hôm nay để giữ streak".</summary>
    public bool StreakSafeToday { get; set; }

    /// <summary>Số ngày có ≥ 1 hoạt động trong khoảng.</summary>
    public int ActiveDays { get; set; }

    public int TotalActions { get; set; }
    public int TotalLessons { get; set; }
    public int TotalQuizzes { get; set; }
}

public class UserActivityDayResponse
{
    /// <summary>yyyy-MM-dd</summary>
    public string Date { get; set; } = null!;
    /// <summary>Tổng mọi loại hoạt động — giá trị tô màu ô.</summary>
    public int Count { get; set; }
    public int Lessons { get; set; }
    public int Quizzes { get; set; }
    /// <summary>Ghi chú + bình luận + câu hỏi + câu trả lời.</summary>
    public int Posts { get; set; }
    public int Enrollments { get; set; }
    public int Certificates { get; set; }
}
