namespace StudyCourseAPI.DTOs.Responses.Admin;

/// <summary>
/// GET api/admin/dashboard?range=7d|30d|90d — mọi số liệu tổng hợp cho trang Tổng quan.
/// Shape bám theo mock FE (src/mockDatas/mockAdminDashboard.ts) để FE thay mock bằng response này
/// mà không đổi component. BE trả số + id; nhãn hiển thị (label/cta) do FE quyết định.
///
/// Chưa có bảng thanh toán nên KPI doanh thu và bước "Mua Pro" trong phễu KHÔNG có trong response —
/// FE ẩn card tương ứng khi thiếu. Thêm khi có bảng Orders.
/// </summary>
public class AdminDashboardResponse
{
    public string Range { get; set; } = "30d";
    /// <summary>Đầu kỳ hiện tại (UTC, inclusive).</summary>
    public DateTime From { get; set; }
    /// <summary>Cuối kỳ hiện tại (UTC, exclusive).</summary>
    public DateTime To { get; set; }

    public List<DashboardKpiResponse> Kpis { get; set; } = new();

    /// <summary>Tỉ lệ hoàn thành toàn hệ thống (% enrollment đã IsCompleted) — snapshot, không theo kỳ.</summary>
    public double CompletionRate { get; set; }

    /// <summary>Chuỗi theo ngày của kỳ hiện tại (đủ ngày, ngày không có dữ liệu = 0).</summary>
    public List<DashboardDailyPointResponse> Daily { get; set; } = new();
    /// <summary>Kỳ liền trước, cùng độ dài — để vẽ overlay so sánh.</summary>
    public List<DashboardDailyPointResponse> DailyPrevious { get; set; } = new();

    public List<FunnelStepResponse> Funnel { get; set; } = new();
    public List<CoursePassRateResponse> PassRates { get; set; } = new();
    public List<LessonDropOffResponse> DropOffs { get; set; } = new();
    public List<HardQuestionResponse> HardQuestions { get; set; } = new();

    /// <summary>[7][24] số phiên học theo thứ (0 = Thứ 2 … 6 = CN) × giờ, múi giờ Việt Nam. Xấp xỉ từ LastWatchedAt.</summary>
    public int[][] StudyHeatmap { get; set; } = Array.Empty<int[]>();

    public List<TopCourseResponse> TopCourses { get; set; } = new();
    public List<LevelSliceResponse> LevelDistribution { get; set; } = new();
    public List<CertificateAdminResponse> RecentCertificates { get; set; } = new();
    public List<TopArticleResponse> TopArticles { get; set; } = new();
}

/// <summary>
/// Một ô KPI. Id cố định: learners | enrollments | completions | certificates | avgScore.
/// </summary>
public class DashboardKpiResponse
{
    public string Id { get; set; } = null!;
    /// <summary>Giá trị kỳ này (đếm, hoặc % với avgScore).</summary>
    public double Value { get; set; }
    /// <summary>Giá trị kỳ liền trước.</summary>
    public double Previous { get; set; }
    /// <summary>% thay đổi so với kỳ trước; 0 khi kỳ trước = 0.</summary>
    public double DeltaPct { get; set; }
    /// <summary>12 điểm sparkline — kỳ hiện tại gom thành 12 bucket bằng nhau.</summary>
    public List<double> Trend { get; set; } = new();
}

public class DashboardDailyPointResponse
{
    /// <summary>yyyy-MM-dd theo múi giờ Việt Nam.</summary>
    public string Date { get; set; } = null!;
    public int Enrollments { get; set; }
    public int Signups { get; set; }
    public int Certificates { get; set; }
    public int Completions { get; set; }
}

/// <summary>Số người (distinct) đi qua mỗi bước. Id: signup | verified | enrolled | finished | cert.</summary>
public class FunnelStepResponse
{
    public string Id { get; set; } = null!;
    public int Value { get; set; }
}

public class CoursePassRateResponse
{
    public long CourseId { get; set; }
    public string Title { get; set; } = null!;
    public int Attempts { get; set; }
    public int Passed { get; set; }
    /// <summary>0–100</summary>
    public double PassRate { get; set; }
}

/// <summary>Bài học nơi nhiều học viên dừng lại nhất (bài hoàn thành cuối cùng của những người chưa xong khóa).</summary>
public class LessonDropOffResponse
{
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long LessonId { get; set; }
    public string LessonTitle { get; set; } = null!;
    /// <summary>Vị trí bài trong khóa, 1-based theo chương rồi bài.</summary>
    public int LessonIndex { get; set; }
    public int LessonTotal { get; set; }
    /// <summary>Số học viên đã hoàn thành bài này.</summary>
    public int Reached { get; set; }
    /// <summary>Số học viên dừng lại tại đây (chưa hoàn thành khóa).</summary>
    public int Stopped { get; set; }
    /// <summary>Stopped / Reached × 100.</summary>
    public double DropPct { get; set; }
}

/// <summary>Câu hỏi quiz bị trả lời sai nhiều nhất, tính từ snapshot đáp án của các lượt làm.</summary>
public class HardQuestionResponse
{
    public long QuestionId { get; set; }
    public string Question { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public int Attempts { get; set; }
    public int Wrong { get; set; }
    /// <summary>0–100</summary>
    public double WrongPct { get; set; }
}

public class TopCourseResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Level { get; set; } = null!;
    /// <summary>Ghi danh trong kỳ.</summary>
    public int Enrollments { get; set; }
}

public class LevelSliceResponse
{
    public string Level { get; set; } = null!;
    public int Count { get; set; }
}

public class TopArticleResponse
{
    public long Id { get; set; }
    public string Title { get; set; } = null!;
    public string Slug { get; set; } = null!;
    /// <summary>Counter cộng dồn — chưa có lịch sử theo kỳ (cần bảng ActivityEvents).</summary>
    public int Views { get; set; }
}
