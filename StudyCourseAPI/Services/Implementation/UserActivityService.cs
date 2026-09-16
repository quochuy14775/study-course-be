using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Data;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

/// <summary>
/// Gom mọi hành động có timestamp của user từ 8 bảng thành "hoạt động theo ngày" (giờ VN),
/// rồi tính streak ở C#. Chỉ đọc; dùng thẳng DbContext vì truy vấn là một UNION ALL thuần SQL.
///
/// Khi có bảng UserDailyActivities (1 dòng / user / ngày, upsert từ service) thì thay UNION này
/// bằng một SELECT đơn giản — phần tính streak giữ nguyên.
/// </summary>
public class UserActivityService : IUserActivityService
{
    private const string Tz = "Asia/Ho_Chi_Minh";
    private static readonly TimeSpan TzOffset = TimeSpan.FromHours(7);

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;

    public UserActivityService(ApplicationDbContext db, ICurrentUser currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public async Task<UserActivityResponse> GetMyActivityAsync(int days)
    {
        days = Math.Clamp(days, 7, 730);
        var userId = _currentUser.GetCurrentUserId();

        var todayLocal = DateOnly.FromDateTime(DateTime.UtcNow + TzOffset);
        var startLocal = todayLocal.AddDays(1 - days);
        var from = ToUtc(startLocal);
        var to = ToUtc(todayLocal.AddDays(1));

        // Mỗi nguồn 1 nhánh UNION ALL; gom theo ngày VN + loại. Cột "IsDeleted" chỉ có ở bảng soft-delete.
        const string sql = """
            SELECT (e.ts AT TIME ZONE 'Asia/Ho_Chi_Minh')::date AS "Day", e.kind AS "Kind", COUNT(*)::int AS "Count"
            FROM (
                SELECT "CompletedAt" AS ts, 'lesson' AS kind FROM "UserLessonProgresses"
                    WHERE "UserId" = {0} AND "IsCompleted" = true AND "IsDeleted" = false AND "CompletedAt" IS NOT NULL
                UNION ALL SELECT "SubmittedAt", 'quiz'   FROM "QuizAttempts"    WHERE "UserId" = {0}
                UNION ALL SELECT "CreatedAt",   'post'   FROM "LessonNotes"     WHERE "UserId" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt",   'post'   FROM "LessonComments"  WHERE "UserId" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt",   'post'   FROM "LessonQuestions" WHERE "UserId" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt",   'post'   FROM "QuestionAnswers" WHERE "UserId" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "EnrolledAt",  'enroll' FROM "UserCourses"     WHERE "UserId" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "IssuedAt",    'cert'   FROM "Certificates"    WHERE "UserId" = {0}
            ) e
            WHERE e.ts >= {1} AND e.ts < {2}
            GROUP BY 1, 2
            """;

        var rows = await _db.Database.SqlQueryRaw<ActivityRow>(sql, userId, from, to).ToListAsync();

        // ── Đủ ngày, ngày trống = 0 ──
        var byDay = ActivityCalendar.EmptyDays(startLocal, days);

        foreach (var r in rows)
        {
            if (!byDay.TryGetValue(r.Day, out var day)) continue;
            switch (r.Kind)
            {
                case "lesson": day.Lessons += r.Count; break;
                case "quiz":   day.Quizzes += r.Count; break;
                case "post":   day.Posts += r.Count; break;
                case "enroll": day.Enrollments += r.Count; break;
                case "cert":   day.Certificates += r.Count; break;
            }
            day.Count += r.Count;
        }

        return ActivityCalendar.Build(byDay, todayLocal, startLocal, days);
    }

    private static DateTime ToUtc(DateOnly localDate) =>
        DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue) - TzOffset, DateTimeKind.Utc);
}

/// <summary>
/// Phần chung cho "hoạt động theo ngày": dựng response + tính streak từ map ngày → count.
/// Dùng cho cả hoạt động học tập (user) lẫn hoạt động quản trị (admin).
/// </summary>
internal static class ActivityCalendar
{
    public static readonly TimeSpan TzOffset = TimeSpan.FromHours(7);

    public static DateTime ToUtc(DateOnly localDate) =>
        DateTime.SpecifyKind(localDate.ToDateTime(TimeOnly.MinValue) - TzOffset, DateTimeKind.Utc);

    /// <summary>Tạo map đủ ngày (count = 0) từ startLocal, `days` ngày.</summary>
    public static Dictionary<DateOnly, UserActivityDayResponse> EmptyDays(DateOnly startLocal, int days)
    {
        var byDay = new Dictionary<DateOnly, UserActivityDayResponse>();
        for (var i = 0; i < days; i++)
        {
            var d = startLocal.AddDays(i);
            byDay[d] = new UserActivityDayResponse { Date = d.ToString("yyyy-MM-dd") };
        }
        return byDay;
    }

    public static UserActivityResponse Build(Dictionary<DateOnly, UserActivityDayResponse> byDay, DateOnly todayLocal, DateOnly startLocal, int days)
    {
        var list = byDay.Values.ToList(); // theo thứ tự chèn = tăng dần

        var active = new HashSet<DateOnly>(byDay.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key));
        var safeToday = active.Contains(todayLocal);

        // Chuỗi hiện tại: lùi từ hôm nay; nếu hôm nay chưa có gì thì lùi từ hôm qua (hôm nay vẫn còn cơ hội giữ)
        var cursor = safeToday ? todayLocal : todayLocal.AddDays(-1);
        var current = 0;
        while (active.Contains(cursor)) { current++; cursor = cursor.AddDays(-1); }

        var longest = 0;
        var run = 0;
        for (var i = 0; i < days; i++)
        {
            run = active.Contains(startLocal.AddDays(i)) ? run + 1 : 0;
            if (run > longest) longest = run;
        }

        return new UserActivityResponse
        {
            Today = todayLocal.ToString("yyyy-MM-dd"),
            Days = list,
            CurrentStreak = current,
            LongestStreak = longest,
            StreakSafeToday = safeToday,
            ActiveDays = active.Count,
            TotalActions = list.Sum(d => d.Count),
            TotalLessons = list.Sum(d => d.Lessons),
            TotalQuizzes = list.Sum(d => d.Quizzes),
        };
    }
}

/// <summary>Row cho SqlQueryRaw — tên property trùng alias cột.</summary>
internal sealed class ActivityRow
{
    public DateOnly Day { get; set; }
    public string Kind { get; set; } = null!;
    public int Count { get; set; }
}
