using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Data;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Services;

/// <summary>
/// Số liệu tổng hợp cho dashboard admin.
///
/// Chỉ đọc và gom từ ~12 bảng, nên nhận thẳng <see cref="ApplicationDbContext"/> thay vì bơm 12 repository
/// (mỗi repository chỉ bọc một DbSet, không thêm giá trị ở đây). Những truy vấn cần múi giờ, jsonb hoặc
/// window function viết SQL thuần qua <c>Database.SqlQueryRaw</c>; phần còn lại dùng LINQ như các service khác.
///
/// Quy ước ngày: mọi bucket "theo ngày / theo giờ" tính theo múi giờ Việt Nam (UTC+7, không có DST).
/// </summary>
public class AdminDashboardService : IAdminDashboardService
{
    private const string Tz = "Asia/Ho_Chi_Minh";
    private static readonly TimeSpan TzOffset = TimeSpan.FromHours(7);

    /// <summary>Ngưỡng tối thiểu để một con số thống kê "có ý nghĩa" (tránh 1/1 = 100%).</summary>
    private const int MinSample = 5;

    private readonly ApplicationDbContext _db;

    public AdminDashboardService(ApplicationDbContext db)
    {
        _db = db;
    }

    // ─────────────────────────────────────────────────────────
    // Dashboard
    // ─────────────────────────────────────────────────────────

    public async Task<AdminDashboardResponse> GetDashboardAsync(string range)
    {
        var days = range switch { "7d" => 7, "90d" => 90, _ => 30 };
        range = days switch { 7 => "7d", 90 => "90d", _ => "30d" };

        // Biên kỳ theo ngày VN, đổi sang UTC để so với cột timestamptz.
        var todayLocal = (DateTime.UtcNow + TzOffset).Date;
        var to = ToUtc(todayLocal.AddDays(1));
        var from = ToUtc(todayLocal.AddDays(1 - days));
        var prevFrom = ToUtc(todayLocal.AddDays(1 - 2 * days));
        var fromLocal = DateOnly.FromDateTime(todayLocal.AddDays(1 - days));
        var prevFromLocal = DateOnly.FromDateTime(todayLocal.AddDays(1 - 2 * days));

        // ── Chuỗi theo ngày, lấy cả 2 kỳ trong 1 truy vấn / bảng ──
        var enroll = await DailyCountsAsync("UserCourses", "EnrolledAt", "\"IsDeleted\" = false", prevFrom, to);
        var signup = await DailyCountsAsync("AspNetUsers", "CreatedAt", "\"IsDeleted\" = false", prevFrom, to);
        var cert = await DailyCountsAsync("Certificates", "IssuedAt", null, prevFrom, to);
        var done = await DailyCountsAsync("UserCourses", "CompletedAt", "\"IsDeleted\" = false AND \"IsCompleted\" = true", prevFrom, to);
        var score = await DailyScoresAsync(prevFrom, to);

        var daily = BuildDaily(fromLocal, days, enroll, signup, cert, done);
        var dailyPrev = BuildDaily(prevFromLocal, days, enroll, signup, cert, done);

        // ── KPI ──
        var kpis = new List<DashboardKpiResponse>
        {
            Kpi("learners", daily.Select(d => (double)d.Signups), dailyPrev.Select(d => (double)d.Signups)),
            Kpi("enrollments", daily.Select(d => (double)d.Enrollments), dailyPrev.Select(d => (double)d.Enrollments)),
            Kpi("completions", daily.Select(d => (double)d.Completions), dailyPrev.Select(d => (double)d.Completions)),
            Kpi("certificates", daily.Select(d => (double)d.Certificates), dailyPrev.Select(d => (double)d.Certificates)),
            AvgScoreKpi(score, fromLocal, prevFromLocal, days),
        };

        var totalEnrollments = await _db.UserCourses.CountAsync();
        var completedEnrollments = await _db.UserCourses.CountAsync(uc => uc.IsCompleted);

        // ── Các khối còn lại ──
        var response = new AdminDashboardResponse
        {
            Range = range,
            From = from,
            To = to,
            Kpis = kpis,
            CompletionRate = totalEnrollments == 0 ? 0 : Round(100.0 * completedEnrollments / totalEnrollments),
            Daily = daily,
            DailyPrevious = dailyPrev,
            Funnel = await FunnelAsync(),
            PassRates = await PassRatesAsync(),
            DropOffs = await DropOffsAsync(limit: 5),
            HardQuestions = await HardQuestionsAsync(since: ToUtc(todayLocal.AddDays(-90)), limit: 6),
            StudyHeatmap = await HeatmapAsync(since: ToUtc(todayLocal.AddDays(-90))),
            TopCourses = await TopCoursesAsync(from, to, limit: 5),
            LevelDistribution = await LevelDistributionAsync(),
            RecentCertificates = await RecentCertificatesAsync(limit: 5),
            TopArticles = await TopArticlesAsync(limit: 5),
        };

        return response;
    }

    // ─────────────────────────────────────────────────────────
    // Inbox
    // ─────────────────────────────────────────────────────────

    public async Task<AdminInboxResponse> GetInboxAsync()
    {
        var now = DateTime.UtcNow;
        var items = new List<AdminActionItemResponse>();

        // Câu hỏi chưa ai trả lời — gom theo khóa
        var unanswered = await _db.LessonQuestions
            .AsNoTracking()
            .Where(q => !q.IsResolved && q.AnswerCount == 0)
            .GroupBy(q => new { q.Lesson.CourseId, q.Lesson.Course.Title })
            .Select(g => new { g.Key.CourseId, g.Key.Title, Count = g.Count(), Oldest = g.Min(q => q.CreatedAt) })
            .ToListAsync();

        items.AddRange(unanswered.Select(u => new AdminActionItemResponse
        {
            Id = $"unanswered-{u.CourseId}",
            Kind = "unanswered-questions",
            Tone = (now - u.Oldest) > TimeSpan.FromHours(48) ? "critical" : "warning",
            Title = $"{u.Count} câu hỏi chưa được trả lời",
            Meta = $"{u.Title} · lâu nhất {Ago(now - u.Oldest)}",
            CourseId = u.CourseId,
            CourseTitle = u.Title,
            Count = u.Count,
        }));

        // Review ≤ 2★ chưa có phản hồi — gom theo khóa
        var lowReviews = await _db.CourseReviews
            .AsNoTracking()
            .Where(r => r.Rating <= 2 && !r.Replies.Any())
            .GroupBy(r => new { r.CourseId, r.Course.Title })
            .Select(g => new { g.Key.CourseId, g.Key.Title, Count = g.Count(), Oldest = g.Min(r => r.CreatedAt) })
            .ToListAsync();

        items.AddRange(lowReviews.Select(r => new AdminActionItemResponse
        {
            Id = $"low-reviews-{r.CourseId}",
            Kind = "low-reviews",
            Tone = "warning",
            Title = $"{r.Count} review thấp chưa được phản hồi",
            Meta = $"{r.Title} · lâu nhất {Ago(now - r.Oldest)}",
            CourseId = r.CourseId,
            CourseTitle = r.Title,
            Count = r.Count,
        }));

        // Khóa đang mở nhưng chưa có bài học nào
        var noLesson = await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsActive && c.LessonCount == 0)
            .Select(c => new { c.Id, c.Title, c.CreatedAt })
            .ToListAsync();

        items.AddRange(noLesson.Select(c => new AdminActionItemResponse
        {
            Id = $"no-lesson-{c.Id}",
            Kind = "no-lesson",
            Tone = "warning",
            Title = "Khóa học chưa có bài học nào",
            Meta = $"{c.Title} · tạo {Ago(now - c.CreatedAt)}",
            CourseId = c.Id,
            CourseTitle = c.Title,
        }));

        // Có bài học nhưng chưa có bài kiểm tra cuối khóa → không cấp được chứng chỉ
        var noTest = await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsActive && c.LessonCount > 0
                        && !_db.Quizzes.Any(q => q.CourseId == c.Id && q.QuizType == QuizType.CourseTest))
            .Select(c => new { c.Id, c.Title, c.Price })
            .ToListAsync();

        items.AddRange(noTest.Select(c => new AdminActionItemResponse
        {
            Id = $"no-test-{c.Id}",
            Kind = "no-test",
            Tone = c.Price > 0 ? "warning" : "info",
            Title = "Chưa có bài kiểm tra cuối khóa",
            Meta = c.Price > 0 ? $"{c.Title} · khóa có phí" : c.Title,
            CourseId = c.Id,
            CourseTitle = c.Title,
        }));

        // Khóa có phí chưa có ảnh bìa
        var noImage = await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsActive && c.Price > 0 && (c.ImageUrl == null || c.ImageUrl == ""))
            .Select(c => new { c.Id, c.Title })
            .ToListAsync();

        items.AddRange(noImage.Select(c => new AdminActionItemResponse
        {
            Id = $"no-image-{c.Id}",
            Kind = "no-image",
            Tone = "info",
            Title = "Khóa có phí chưa có ảnh bìa",
            Meta = c.Title,
            CourseId = c.Id,
            CourseTitle = c.Title,
        }));

        // Khóa đang ẩn mà vẫn có học viên
        var hidden = await _db.Courses
            .AsNoTracking()
            .Where(c => !c.IsActive)
            .Select(c => new { c.Id, c.Title, c.UpdatedAt, c.CreatedAt, Learners = c.UserCourses.Count(uc => !uc.IsCompleted) })
            .ToListAsync();

        items.AddRange(hidden.Select(c => new AdminActionItemResponse
        {
            Id = $"hidden-{c.Id}",
            Kind = "hidden",
            Tone = c.Learners > 0 ? "warning" : "info",
            Title = $"Khóa học bị ẩn {Ago(now - (c.UpdatedAt ?? c.CreatedAt))}",
            Meta = c.Learners > 0 ? $"{c.Title} · {c.Learners} học viên đang học" : c.Title,
            CourseId = c.Id,
            CourseTitle = c.Title,
            Count = c.Learners,
        }));

        var toneOrder = new Dictionary<string, int> { ["critical"] = 0, ["warning"] = 1, ["info"] = 2 };

        // ── Danh sách chi tiết cho 2 tab còn lại ──
        var openQuestions = await _db.LessonQuestions
            .AsNoTracking()
            .Where(q => !q.IsResolved && q.AnswerCount == 0)
            .OrderBy(q => q.CreatedAt)
            .Take(10)
            .Select(q => new OpenQuestionResponse
            {
                Id = q.Id,
                UserId = q.UserId,
                UserName = q.User.FullName ?? q.User.UserName ?? q.User.Email ?? "Học viên",
                CourseId = q.Lesson.CourseId,
                CourseTitle = q.Lesson.Course.Title,
                LessonId = q.LessonId,
                LessonTitle = q.Lesson.Title,
                Content = q.Content,
                AnswerCount = q.AnswerCount,
                CreatedAt = q.CreatedAt,
            })
            .ToListAsync();

        var lowReviewList = await _db.CourseReviews
            .AsNoTracking()
            .Where(r => r.Rating <= 2 && !r.Replies.Any())
            .OrderByDescending(r => r.CreatedAt)
            .Take(10)
            .Select(r => new LowReviewResponse
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User.FullName ?? r.User.UserName ?? r.User.Email ?? "Học viên",
                CourseId = r.CourseId,
                CourseTitle = r.Course.Title,
                Rating = r.Rating,
                Content = r.Content,
                CreatedAt = r.CreatedAt,
            })
            .ToListAsync();

        return new AdminInboxResponse
        {
            ActionItems = items
                .OrderBy(i => toneOrder[i.Tone])
                .ThenByDescending(i => i.Count)
                .Take(20)
                .ToList(),
            OpenQuestions = openQuestions,
            LowReviews = lowReviewList,
        };
    }

    // ─────────────────────────────────────────────────────────
    // Activity
    // ─────────────────────────────────────────────────────────

    public async Task<AdminActivityResponse> GetActivityAsync(DateTime? before, int limit)
    {
        limit = Math.Clamp(limit, 1, 50);
        var cursor = before.HasValue ? DateTime.SpecifyKind(before.Value, DateTimeKind.Utc) : DateTime.UtcNow.AddMinutes(1);

        // Mỗi nguồn lấy `limit` bản ghi mới nhất trước cursor (SQL), ghép Id chuỗi ở C#, rồi trộn và cắt lại.
        var enrolls = (await _db.UserCourses.AsNoTracking()
            .Where(x => x.EnrolledAt < cursor)
            .OrderByDescending(x => x.EnrolledAt).Take(limit)
            .Select(x => new
            {
                x.UserId, x.CourseId, x.Course.Title, At = x.EnrolledAt,
                Actor = x.User.FullName ?? x.User.UserName ?? x.User.Email ?? "Học viên",
            }).ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"enroll:{x.UserId}:{x.CourseId}", Kind = "enroll", ActorName = x.Actor,
                CourseId = x.CourseId, CourseTitle = x.Title, CreatedAt = x.At,
            });

        var certs = (await _db.Certificates.AsNoTracking()
            .Where(x => x.IssuedAt < cursor)
            .OrderByDescending(x => x.IssuedAt).Take(limit)
            .Select(x => new
            {
                x.Id, x.CourseId, x.Course.Title, x.ScorePercentage, At = x.IssuedAt,
                Actor = x.User.FullName ?? x.User.UserName ?? x.User.Email ?? "Học viên",
            }).ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"cert_issued:{x.Id}", Kind = "cert_issued", ActorName = x.Actor,
                CourseId = x.CourseId, CourseTitle = x.Title, Value = x.ScorePercentage, CreatedAt = x.At,
            });

        var reviews = (await _db.CourseReviews.AsNoTracking()
            .Where(x => x.CreatedAt < cursor)
            .OrderByDescending(x => x.CreatedAt).Take(limit)
            .Select(x => new
            {
                x.Id, x.CourseId, x.Course.Title, x.Rating, At = x.CreatedAt,
                Actor = x.User.FullName ?? x.User.UserName ?? x.User.Email ?? "Học viên",
            }).ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"review:{x.Id}", Kind = "review", ActorName = x.Actor,
                CourseId = x.CourseId, CourseTitle = x.Title, Value = x.Rating, CreatedAt = x.At,
            });

        var questions = (await _db.LessonQuestions.AsNoTracking()
            .Where(x => x.CreatedAt < cursor)
            .OrderByDescending(x => x.CreatedAt).Take(limit)
            .Select(x => new
            {
                x.Id, x.LessonId, x.Lesson.CourseId, x.Lesson.Course.Title, At = x.CreatedAt,
                Actor = x.User.FullName ?? x.User.UserName ?? x.User.Email ?? "Học viên",
            }).ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"question:{x.Id}", Kind = "question", ActorName = x.Actor,
                CourseId = x.CourseId, CourseTitle = x.Title, LessonId = x.LessonId, CreatedAt = x.At,
            });

        var created = (await _db.Courses.AsNoTracking()
            .Where(x => x.CreatedAt < cursor)
            .OrderByDescending(x => x.CreatedAt).Take(limit)
            .Select(x => new { x.Id, x.Title, x.CreatedBy, At = x.CreatedAt })
            .ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"course_created:{x.Id}", Kind = "course_created", ActorName = x.CreatedBy ?? "Admin",
                CourseId = x.Id, CourseTitle = x.Title, CreatedAt = x.At,
            });

        var updated = (await _db.Courses.AsNoTracking()
            .Where(x => x.UpdatedAt != null && x.UpdatedAt < cursor)
            .OrderByDescending(x => x.UpdatedAt).Take(limit)
            .Select(x => new { x.Id, x.Title, x.CreatedBy, x.UpdatedBy, At = x.UpdatedAt!.Value })
            .ToListAsync())
            .Select(x => new AdminActivityItemResponse
            {
                Id = $"course_updated:{x.Id}", Kind = "course_updated", ActorName = x.UpdatedBy ?? x.CreatedBy ?? "Admin",
                CourseId = x.Id, CourseTitle = x.Title, CreatedAt = x.At,
            });

        var items = enrolls.Concat(certs).Concat(reviews).Concat(questions).Concat(created).Concat(updated)
            .OrderByDescending(i => i.CreatedAt)
            .Take(limit)
            .ToList();

        return new AdminActivityResponse
        {
            Items = items,
            NextCursor = items.Count == limit ? items[^1].CreatedAt : null,
        };
    }

    // ─────────────────────────────────────────────────────────
    // Khối con của dashboard
    // ─────────────────────────────────────────────────────────

    private async Task<List<FunnelStepResponse>> FunnelAsync()
    {
        var signup = await _db.Users.CountAsync(u => !u.IsDeleted);
        var verified = await _db.Users.CountAsync(u => !u.IsDeleted && u.EmailConfirmed);
        var enrolled = await _db.UserCourses.Select(uc => uc.UserId).Distinct().CountAsync();
        var finished = await _db.UserCourses.Where(uc => uc.IsCompleted).Select(uc => uc.UserId).Distinct().CountAsync();
        var cert = await _db.Certificates.Select(c => c.UserId).Distinct().CountAsync();

        return new List<FunnelStepResponse>
        {
            new() { Id = "signup",   Value = signup },
            new() { Id = "verified", Value = verified },
            new() { Id = "enrolled", Value = enrolled },
            new() { Id = "finished", Value = finished },
            new() { Id = "cert",     Value = cert },
        };
    }

    private async Task<List<CoursePassRateResponse>> PassRatesAsync()
    {
        var rows = await _db.QuizAttempts
            .AsNoTracking()
            .Where(a => a.Quiz.QuizType == QuizType.CourseTest)
            .GroupBy(a => new { a.Quiz.CourseId, a.Quiz.Course.Title })
            .Select(g => new CoursePassRateResponse
            {
                CourseId = g.Key.CourseId,
                Title = g.Key.Title,
                Attempts = g.Count(),
                Passed = g.Count(a => a.IsPassed),
            })
            .Where(x => x.Attempts >= MinSample)
            .OrderByDescending(x => x.Attempts)
            .Take(8)
            .ToListAsync();

        foreach (var r in rows)
            r.PassRate = Round(100.0 * r.Passed / r.Attempts);

        return rows.OrderBy(r => r.PassRate).ToList();
    }

    private async Task<List<LessonDropOffResponse>> DropOffsAsync(int limit)
    {
        // "Bài hoàn thành cuối cùng" của mỗi (user, khóa) chưa hoàn thành khóa = nơi người đó dừng lại.
        // Bỏ bài cuối của khóa vì dừng ở đó nghĩa là đã học hết, chỉ chưa qua test.
        const string sql = """
            WITH last_done AS (
                SELECT DISTINCT ON (p."UserId", p."CourseId") p."UserId", p."CourseId", p."LessonId"
                FROM "UserLessonProgresses" p
                JOIN "UserCourses" uc ON uc."UserId" = p."UserId" AND uc."CourseId" = p."CourseId"
                                      AND uc."IsDeleted" = false AND uc."IsCompleted" = false
                WHERE p."IsCompleted" = true AND p."IsDeleted" = false
                ORDER BY p."UserId", p."CourseId", p."CompletedAt" DESC NULLS LAST
            ),
            stopped AS (
                SELECT "LessonId", COUNT(*)::int AS "Stopped" FROM last_done GROUP BY "LessonId"
            ),
            reached AS (
                SELECT "LessonId", COUNT(*)::int AS "Reached"
                FROM "UserLessonProgresses"
                WHERE "IsCompleted" = true AND "IsDeleted" = false
                GROUP BY "LessonId"
            ),
            ordered AS (
                SELECT l."Id",
                       ROW_NUMBER() OVER (PARTITION BY l."CourseId" ORDER BY ch."OrderIndex" NULLS LAST, l."OrderIndex", l."Id")::int AS "LessonIndex"
                FROM "Lessons" l
                LEFT JOIN "Chapters" ch ON ch."Id" = l."ChapterId"
                WHERE l."IsDeleted" = false
            )
            SELECT l."CourseId", c."Title" AS "CourseTitle", l."Id" AS "LessonId", l."Title" AS "LessonTitle",
                   o."LessonIndex", c."LessonCount" AS "LessonTotal", r."Reached", s."Stopped"
            FROM stopped s
            JOIN reached r ON r."LessonId" = s."LessonId"
            JOIN "Lessons" l ON l."Id" = s."LessonId" AND l."IsDeleted" = false
            JOIN "Courses" c ON c."Id" = l."CourseId" AND c."IsDeleted" = false
            JOIN ordered o ON o."Id" = l."Id"
            WHERE r."Reached" >= {0} AND o."LessonIndex" < c."LessonCount"
            ORDER BY (s."Stopped"::float8 / r."Reached") DESC, r."Reached" DESC
            LIMIT {1}
            """;

        var rows = await _db.Database.SqlQueryRaw<DropOffRow>(sql, MinSample, limit).ToListAsync();

        return rows.Select(r => new LessonDropOffResponse
        {
            CourseId = r.CourseId,
            CourseTitle = r.CourseTitle,
            LessonId = r.LessonId,
            LessonTitle = r.LessonTitle,
            LessonIndex = r.LessonIndex,
            LessonTotal = r.LessonTotal,
            Reached = r.Reached,
            Stopped = r.Stopped,
            DropPct = Round(100.0 * r.Stopped / r.Reached),
        }).ToList();
    }

    private async Task<List<HardQuestionResponse>> HardQuestionsAsync(DateTime since, int limit)
    {
        // Answers là jsonb snapshot [{QuestionId, QuestionContent, IsCorrect, ...}] — bung ra bằng jsonb_array_elements.
        const string sql = """
            SELECT (ans->>'QuestionId')::bigint AS "QuestionId",
                   MAX(ans->>'QuestionContent')  AS "Question",
                   q."CourseId",
                   MAX(c."Title")                 AS "CourseTitle",
                   COUNT(*)::int                  AS "Attempts",
                   SUM(CASE WHEN (ans->>'IsCorrect')::boolean THEN 0 ELSE 1 END)::int AS "Wrong"
            FROM "QuizAttempts" a
            CROSS JOIN LATERAL jsonb_array_elements(a."Answers") AS ans
            JOIN "Quizzes" q ON q."Id" = a."QuizId" AND q."IsDeleted" = false
            JOIN "Courses" c ON c."Id" = q."CourseId" AND c."IsDeleted" = false
            WHERE a."SubmittedAt" >= {0}
            GROUP BY 1, q."CourseId"
            HAVING COUNT(*) >= {1}
            ORDER BY (SUM(CASE WHEN (ans->>'IsCorrect')::boolean THEN 0 ELSE 1 END)::float8 / COUNT(*)) DESC, COUNT(*) DESC
            LIMIT {2}
            """;

        var rows = await _db.Database.SqlQueryRaw<HardQuestionRow>(sql, since, MinSample, limit).ToListAsync();

        return rows.Select(r => new HardQuestionResponse
        {
            QuestionId = r.QuestionId,
            Question = r.Question,
            CourseId = r.CourseId,
            CourseTitle = r.CourseTitle,
            Attempts = r.Attempts,
            Wrong = r.Wrong,
            WrongPct = Round(100.0 * r.Wrong / r.Attempts),
        }).ToList();
    }

    private async Task<int[][]> HeatmapAsync(DateTime since)
    {
        // Xấp xỉ: LastWatchedAt là lần xem cuối của mỗi (user, bài), không phải từng phiên học.
        // Postgres DOW: 0 = CN … 6 = T7 → đổi về 0 = T2 … 6 = CN cho FE.
        // $$""" : {{x}} là interpolation, {0} giữ nguyên làm placeholder tham số cho SqlQueryRaw
        var sql = $$"""
            SELECT EXTRACT(DOW  FROM ("LastWatchedAt" AT TIME ZONE '{{Tz}}'))::int AS "Dow",
                   EXTRACT(HOUR FROM ("LastWatchedAt" AT TIME ZONE '{{Tz}}'))::int AS "Hour",
                   COUNT(*)::int AS "Count"
            FROM "UserLessonProgresses"
            WHERE "LastWatchedAt" IS NOT NULL AND "LastWatchedAt" >= {0} AND "IsDeleted" = false
            GROUP BY 1, 2
            """;

        var rows = await _db.Database.SqlQueryRaw<HeatRow>(sql, since).ToListAsync();

        var grid = Enumerable.Range(0, 7).Select(_ => new int[24]).ToArray();
        foreach (var r in rows)
        {
            var day = (r.Dow + 6) % 7;
            if (r.Hour is >= 0 and < 24) grid[day][r.Hour] = r.Count;
        }
        return grid;
    }

    private async Task<List<TopCourseResponse>> TopCoursesAsync(DateTime from, DateTime to, int limit)
    {
        return await _db.UserCourses
            .AsNoTracking()
            .Where(uc => uc.EnrolledAt >= from && uc.EnrolledAt < to)
            .GroupBy(uc => new { uc.CourseId, uc.Course.Title, uc.Course.Level })
            .Select(g => new TopCourseResponse
            {
                Id = g.Key.CourseId,
                Title = g.Key.Title,
                Level = g.Key.Level.ToString(),
                Enrollments = g.Count(),
            })
            .OrderByDescending(x => x.Enrollments)
            .Take(limit)
            .ToListAsync();
    }

    private async Task<List<LevelSliceResponse>> LevelDistributionAsync()
    {
        var rows = await _db.Courses
            .AsNoTracking()
            .Where(c => c.IsActive)
            .GroupBy(c => c.Level)
            .Select(g => new { Level = g.Key, Count = g.Count() })
            .ToListAsync();

        // Luôn trả đủ 3 bậc theo thứ tự để FE vẽ ordinal ramp ổn định
        return Enum.GetValues<CourseLevel>()
            .Select(level => new LevelSliceResponse
            {
                Level = level.ToString(),
                Count = rows.FirstOrDefault(r => r.Level == level)?.Count ?? 0,
            })
            .ToList();
    }

    private async Task<List<CertificateAdminResponse>> RecentCertificatesAsync(int limit)
    {
        var items = await _db.Certificates
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .OrderByDescending(c => c.IssuedAt)
            .Take(limit)
            .ToListAsync();

        return items.Select(c => new CertificateAdminResponse(c)).ToList();
    }

    private async Task<List<TopArticleResponse>> TopArticlesAsync(int limit)
    {
        return await _db.Articles
            .AsNoTracking()
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.ViewCount)
            .Take(limit)
            .Select(a => new TopArticleResponse { Id = a.Id, Title = a.Title, Slug = a.Slug, Views = a.ViewCount })
            .ToListAsync();
    }

    // ─────────────────────────────────────────────────────────
    // Chuỗi theo ngày
    // ─────────────────────────────────────────────────────────

    /// <summary>Đếm bản ghi theo ngày VN trong [from, to). Tên bảng/cột là hằng trong code, không phải input.</summary>
    private async Task<Dictionary<DateOnly, int>> DailyCountsAsync(string table, string column, string? extraWhere, DateTime from, DateTime to)
    {
        var where = extraWhere is null ? "" : $" AND {extraWhere}";
        var sql = $$"""
            SELECT ("{{column}}" AT TIME ZONE '{{Tz}}')::date AS "Day", COUNT(*)::int AS "Count"
            FROM "{{table}}"
            WHERE "{{column}}" >= {0} AND "{{column}}" < {1}{{where}}
            GROUP BY 1
            """;

        var rows = await _db.Database.SqlQueryRaw<DayCountRow>(sql, from, to).ToListAsync();
        return rows.ToDictionary(r => r.Day, r => r.Count);
    }

    /// <summary>Điểm trung bình bài test cuối khóa theo ngày (kèm số lượt để tính bình quân gia quyền).</summary>
    private async Task<Dictionary<DateOnly, (double Avg, int Count)>> DailyScoresAsync(DateTime from, DateTime to)
    {
        var sql = $$"""
            SELECT (a."SubmittedAt" AT TIME ZONE '{{Tz}}')::date AS "Day",
                   AVG(a."PercentageScore")::float8 AS "Avg",
                   COUNT(*)::int AS "Count"
            FROM "QuizAttempts" a
            JOIN "Quizzes" q ON q."Id" = a."QuizId" AND q."IsDeleted" = false
            WHERE q."QuizType" = {{(int)QuizType.CourseTest}} AND a."SubmittedAt" >= {0} AND a."SubmittedAt" < {1}
            GROUP BY 1
            """;

        var rows = await _db.Database.SqlQueryRaw<DayAvgRow>(sql, from, to).ToListAsync();
        return rows.ToDictionary(r => r.Day, r => (r.Avg, r.Count));
    }

    private static List<DashboardDailyPointResponse> BuildDaily(
        DateOnly start, int days,
        Dictionary<DateOnly, int> enroll, Dictionary<DateOnly, int> signup,
        Dictionary<DateOnly, int> cert, Dictionary<DateOnly, int> done)
    {
        return Enumerable.Range(0, days)
            .Select(i => start.AddDays(i))
            .Select(d => new DashboardDailyPointResponse
            {
                Date = d.ToString("yyyy-MM-dd"),
                Enrollments = enroll.GetValueOrDefault(d),
                Signups = signup.GetValueOrDefault(d),
                Certificates = cert.GetValueOrDefault(d),
                Completions = done.GetValueOrDefault(d),
            })
            .ToList();
    }

    // ─────────────────────────────────────────────────────────
    // KPI helpers
    // ─────────────────────────────────────────────────────────

    private static DashboardKpiResponse Kpi(string id, IEnumerable<double> current, IEnumerable<double> previous)
    {
        var cur = current.ToList();
        var value = cur.Sum();
        var prev = previous.Sum();
        return new DashboardKpiResponse
        {
            Id = id,
            Value = value,
            Previous = prev,
            DeltaPct = Delta(value, prev),
            Trend = Bucket12(cur, sum: true),
        };
    }

    private static DashboardKpiResponse AvgScoreKpi(
        Dictionary<DateOnly, (double Avg, int Count)> score, DateOnly from, DateOnly prevFrom, int days)
    {
        static (double Avg, List<double> Daily) Period(Dictionary<DateOnly, (double Avg, int Count)> s, DateOnly start, int n)
        {
            var daily = Enumerable.Range(0, n).Select(i => s.GetValueOrDefault(start.AddDays(i))).ToList();
            var attempts = daily.Sum(d => d.Count);
            var avg = attempts == 0 ? 0 : daily.Sum(d => d.Avg * d.Count) / attempts;
            // ngày không có lượt làm → giữ đường sparkline ở mức trung bình kỳ thay vì rơi về 0
            return (avg, daily.Select(d => d.Count == 0 ? avg : d.Avg).ToList());
        }

        var (curAvg, curDaily) = Period(score, from, days);
        var (prevAvg, _) = Period(score, prevFrom, days);

        return new DashboardKpiResponse
        {
            Id = "avgScore",
            Value = Round(curAvg),
            Previous = Round(prevAvg),
            DeltaPct = Delta(curAvg, prevAvg),
            Trend = Bucket12(curDaily, sum: false),
        };
    }

    /// <summary>Gom chuỗi ngày thành 12 điểm bằng nhau (cộng dồn cho số đếm, trung bình cho tỉ lệ).</summary>
    private static List<double> Bucket12(List<double> daily, bool sum)
    {
        if (daily.Count == 0) return Enumerable.Repeat(0.0, 12).ToList();
        var size = Math.Max(1, (int)Math.Ceiling(daily.Count / 12.0));
        var buckets = daily.Chunk(size).Select(chunk => sum ? chunk.Sum() : Round(chunk.Average())).ToList();
        // chuỗi 7 ngày cho ra 7 bucket — bù về 12 bằng cách lặp điểm cuối để FE luôn có đủ 12
        while (buckets.Count < 12) buckets.Add(buckets[^1]);
        return buckets.Take(12).ToList();
    }

    private static double Delta(double current, double previous) =>
        previous == 0 ? 0 : Round(100.0 * (current - previous) / previous);

    private static double Round(double v) => Math.Round(v, 1);

    private static DateTime ToUtc(DateTime localMidnight) =>
        DateTime.SpecifyKind(localMidnight - TzOffset, DateTimeKind.Utc);

    private static string Ago(TimeSpan span)
    {
        if (span.TotalHours < 1) return "vừa xong";
        if (span.TotalHours < 24) return $"{(int)span.TotalHours} giờ trước";
        return $"{(int)span.TotalDays} ngày trước";
    }
}

// ── Row types cho SqlQueryRaw (tên property phải trùng alias cột) ──

internal sealed class DayCountRow
{
    public DateOnly Day { get; set; }
    public int Count { get; set; }
}

internal sealed class DayAvgRow
{
    public DateOnly Day { get; set; }
    public double Avg { get; set; }
    public int Count { get; set; }
}

internal sealed class HeatRow
{
    public int Dow { get; set; }
    public int Hour { get; set; }
    public int Count { get; set; }
}

internal sealed class DropOffRow
{
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long LessonId { get; set; }
    public string LessonTitle { get; set; } = null!;
    public int LessonIndex { get; set; }
    public int LessonTotal { get; set; }
    public int Reached { get; set; }
    public int Stopped { get; set; }
}

internal sealed class HardQuestionRow
{
    public long QuestionId { get; set; }
    public string Question { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public int Attempts { get; set; }
    public int Wrong { get; set; }
}
