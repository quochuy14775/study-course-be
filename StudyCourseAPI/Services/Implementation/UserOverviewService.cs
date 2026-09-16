using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Data;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

/// <summary>
/// Gom số liệu trang cá nhân từ enrollment, tiến độ bài học, quiz, chứng chỉ, tag ngôn ngữ/framework.
/// Chỉ đọc, một user, dữ liệu nhỏ → dùng thẳng DbContext; streak lấy từ <see cref="IUserActivityService"/>.
/// </summary>
public class UserOverviewService : IUserOverviewService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserActivityService _activityService;

    public UserOverviewService(
        ApplicationDbContext db,
        ICurrentUser currentUser,
        UserManager<ApplicationUser> userManager,
        IUserActivityService activityService)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
        _activityService = activityService;
    }

    public async Task<UserOverviewResponse?> GetMyOverviewAsync()
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return null;
        var userId = user.Id;

        var roles = await _userManager.GetRolesAsync(user);
        var activity = await _activityService.GetMyActivityAsync(365);

        // ── Enrollment ──
        var enrollments = await _db.UserCourses
            .AsNoTracking()
            .Where(uc => uc.UserId == userId)
            .Select(uc => new
            {
                uc.CourseId, uc.Progress, uc.IsCompleted, uc.EnrolledAt,
                uc.Course.Title, uc.Course.ImageUrl, uc.Course.Level, uc.Course.LessonCount,
                CourseActive = uc.Course.IsActive,
            })
            .ToListAsync();

        // ── Bài học ──
        var progress = await _db.UserLessonProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsCompleted)
            .Select(p => new { p.LessonId, p.CourseId, p.CompletedAt, p.LastWatchedAt, Duration = p.Lesson.Duration ?? 0, LessonTitle = p.Lesson.Title, CourseTitle = p.Lesson.Course.Title })
            .ToListAsync();

        // ── Quiz ──
        var attempts = await _db.QuizAttempts
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .Select(a => new { a.Id, a.PercentageScore, a.IsPassed, a.SubmittedAt, a.Quiz.Title, a.Quiz.CourseId, CourseTitle = a.Quiz.Course.Title })
            .ToListAsync();

        // ── Chứng chỉ ──
        var certificates = await _db.Certificates
            .AsNoTracking()
            .Include(c => c.Course)
            .Include(c => c.User)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.IssuedAt)
            .ToListAsync();

        // ── Stats ──
        var completedCourses = enrollments.Count(e => e.IsCompleted);
        var quizPassed = attempts.Count(a => a.IsPassed);
        var stats = new UserStatsResponse
        {
            EnrolledCourses = enrollments.Count,
            CompletedCourses = completedCourses,
            CompletionRate = enrollments.Count == 0 ? 0 : Round(100.0 * completedCourses / enrollments.Count),
            LessonsCompleted = progress.Count,
            StudySeconds = progress.Sum(p => p.Duration),
            QuizAttempts = attempts.Count,
            QuizPassed = quizPassed,
            QuizPassRate = attempts.Count == 0 ? 0 : Round(100.0 * quizPassed / attempts.Count),
            BestScore = attempts.Count == 0 ? 0 : Round(attempts.Max(a => a.PercentageScore)),
            Certificates = certificates.Count,
            ActiveDays = activity.ActiveDays,
        };

        // ── Điểm & hạng ──
        var points = progress.Count * 10
                     + quizPassed * 20
                     + (attempts.Count - quizPassed) * 5
                     + certificates.Count * 100
                     + activity.ActiveDays * 5;

        return new UserOverviewResponse
        {
            Profile = UserProfileResponse.UserProfile(user, roles),
            JoinedAt = user.CreatedAt,
            Stats = stats,
            Points = points,
            Rank = RankOf(points),
            CurrentStreak = activity.CurrentStreak,
            LongestStreak = activity.LongestStreak,
            StreakSafeToday = activity.StreakSafeToday,
            Skills = await SkillsAsync(userId),
            Certificates = certificates.Take(6).Select(c => new CertificateResponse(c)).ToList(),
            ContinueLearning = await ContinueLearningAsync(userId, enrollments
                .Where(e => !e.IsCompleted && e.CourseActive)
                .Select(e => (e.CourseId, e.Title, e.ImageUrl, e.Level.ToString(), e.Progress, e.LessonCount))
                .ToList()),
            Recent = BuildRecent(progress.Select(p => new RecentEventResponse
                    {
                        Kind = "lesson", Title = p.LessonTitle, CourseId = p.CourseId, CourseTitle = p.CourseTitle,
                        LessonId = p.LessonId, At = p.CompletedAt ?? p.LastWatchedAt ?? DateTime.MinValue,
                    })
                    .Concat(attempts.Select(a => new RecentEventResponse
                    {
                        Kind = "quiz", Title = a.Title, CourseId = a.CourseId, CourseTitle = a.CourseTitle,
                        Value = Round(a.PercentageScore), Passed = a.IsPassed, At = a.SubmittedAt,
                    }))
                    .Concat(certificates.Select(c => new RecentEventResponse
                    {
                        Kind = "cert", Title = c.Course?.Title ?? "Chứng chỉ", CourseId = c.CourseId, CourseTitle = c.Course?.Title ?? "",
                        Value = Round(c.ScorePercentage), At = c.IssuedAt,
                    }))
                    .Concat(enrollments.Select(e => new RecentEventResponse
                    {
                        Kind = "enroll", Title = e.Title, CourseId = e.CourseId, CourseTitle = e.Title, At = e.EnrolledAt,
                    }))),
            Achievements = BuildAchievements(stats, activity.LongestStreak),
        };
    }

    // ─────────────────────────────────────────────────────────
    // Kỹ năng: % bài học hoàn thành theo ngôn ngữ / framework của khóa
    // ─────────────────────────────────────────────────────────

    private async Task<List<SkillProgressResponse>> SkillsAsync(long userId)
    {
        var languages = await _db.CourseLanguages
            .AsNoTracking()
            .Where(cl => cl.Course.IsActive && cl.Language.IsActive)
            .Select(cl => new
            {
                cl.LanguageId, cl.Language.Name, cl.Language.Slug, cl.Language.IconUrl,
                Total = cl.Course.Lessons.Count(l => l.IsActive),
                Done = cl.Course.Lessons.Count(l => l.IsActive && l.Progresses.Any(p => p.UserId == userId && p.IsCompleted)),
            })
            .ToListAsync();

        var frameworks = await _db.CourseFrameworks
            .AsNoTracking()
            .Where(cf => cf.Course.IsActive && cf.Framework.IsActive)
            .Select(cf => new
            {
                cf.FrameworkId, cf.Framework.Name, cf.Framework.Slug, cf.Framework.IconUrl,
                Total = cf.Course.Lessons.Count(l => l.IsActive),
                Done = cf.Course.Lessons.Count(l => l.IsActive && l.Progresses.Any(p => p.UserId == userId && p.IsCompleted)),
            })
            .ToListAsync();

        var skills = languages
            .GroupBy(x => new { Id = x.LanguageId, x.Name, x.Slug, x.IconUrl })
            .Select(g => Skill("language", g.Key.Id, g.Key.Name, g.Key.Slug, g.Key.IconUrl, g.Sum(x => x.Done), g.Sum(x => x.Total)))
            .Concat(frameworks
                .GroupBy(x => new { Id = x.FrameworkId, x.Name, x.Slug, x.IconUrl })
                .Select(g => Skill("framework", g.Key.Id, g.Key.Name, g.Key.Slug, g.Key.IconUrl, g.Sum(x => x.Done), g.Sum(x => x.Total))))
            .Where(s => s.TotalLessons > 0)
            // Kỹ năng đã có tiến độ lên trước, rồi tới kỹ năng có nhiều bài nhất
            .OrderByDescending(s => s.CompletedLessons > 0)
            .ThenByDescending(s => s.Progress)
            .ThenByDescending(s => s.TotalLessons)
            .Take(8)
            .ToList();

        return skills;
    }

    private static SkillProgressResponse Skill(string kind, long id, string name, string slug, string? icon, int done, int total) => new()
    {
        Kind = kind, Id = id, Name = name, Slug = slug, IconUrl = icon,
        CompletedLessons = done, TotalLessons = total,
        Progress = total == 0 ? 0 : Round(100.0 * done / total),
    };

    // ─────────────────────────────────────────────────────────
    // Tiếp tục học: khóa đang dở, sắp theo lần học gần nhất, kèm bài tiếp theo
    // ─────────────────────────────────────────────────────────

    private async Task<List<ContinueCourseResponse>> ContinueLearningAsync(
        long userId,
        List<(long CourseId, string Title, string? ImageUrl, string Level, double Progress, int LessonCount)> inProgress)
    {
        if (inProgress.Count == 0) return new();

        var courseIds = inProgress.Select(c => c.CourseId).ToList();

        var lastActivity = await _db.UserLessonProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && courseIds.Contains(p.CourseId))
            .GroupBy(p => p.CourseId)
            .Select(g => new { CourseId = g.Key, Last = g.Max(p => p.LastWatchedAt) })
            .ToListAsync();
        var lastByCourse = lastActivity.ToDictionary(x => x.CourseId, x => x.Last);

        var top = inProgress
            .OrderByDescending(c => lastByCourse.GetValueOrDefault(c.CourseId) ?? DateTime.MinValue)
            .Take(4)
            .ToList();
        var topIds = top.Select(c => c.CourseId).ToList();

        // Bài học theo thứ tự chương → bài, và những bài đã xong
        var lessons = await _db.Lessons
            .AsNoTracking()
            .Where(l => topIds.Contains(l.CourseId) && l.IsActive)
            .Select(l => new { l.Id, l.CourseId, l.Title, ChapterOrder = (int?)l.Chapter!.OrderIndex, l.OrderIndex })
            .ToListAsync();

        var done = (await _db.UserLessonProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && topIds.Contains(p.CourseId) && p.IsCompleted)
            .Select(p => p.LessonId)
            .ToListAsync()).ToHashSet();

        return top.Select(c =>
        {
            var ordered = lessons
                .Where(l => l.CourseId == c.CourseId)
                .OrderBy(l => l.ChapterOrder ?? int.MaxValue)
                .ThenBy(l => l.OrderIndex)
                .ThenBy(l => l.Id)
                .ToList();
            var next = ordered.FirstOrDefault(l => !done.Contains(l.Id));

            return new ContinueCourseResponse
            {
                CourseId = c.CourseId,
                Title = c.Title,
                ImageUrl = c.ImageUrl,
                Level = c.Level,
                Progress = Round(c.Progress),
                LessonCount = c.LessonCount,
                CompletedLessons = ordered.Count(l => done.Contains(l.Id)),
                NextLessonId = next?.Id,
                NextLessonTitle = next?.Title,
                LastActivityAt = lastByCourse.GetValueOrDefault(c.CourseId),
            };
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────
    // Hoạt động gần đây, thành tích, hạng
    // ─────────────────────────────────────────────────────────

    private static List<RecentEventResponse> BuildRecent(IEnumerable<RecentEventResponse> events) =>
        events.Where(e => e.At > DateTime.MinValue).OrderByDescending(e => e.At).Take(8).ToList();

    private static List<AchievementResponse> BuildAchievements(UserStatsResponse s, int longestStreak)
    {
        AchievementResponse A(string id, string label, string desc, int progress, int target) => new()
        {
            Id = id, Label = label, Description = desc,
            Progress = Math.Min(progress, target), Target = target, Earned = progress >= target,
        };

        return new List<AchievementResponse>
        {
            A("first-lesson",  "Bước đầu tiên",   "Hoàn thành bài học đầu tiên",          s.LessonsCompleted, 1),
            A("lessons-25",    "Chăm chỉ",        "Hoàn thành 25 bài học",                 s.LessonsCompleted, 25),
            A("lessons-100",   "Cày cuốc",        "Hoàn thành 100 bài học",                s.LessonsCompleted, 100),
            A("streak-7",      "Tuần rực lửa",    "Học 7 ngày liên tiếp",                  longestStreak, 7),
            A("streak-30",     "Kỷ luật thép",    "Học 30 ngày liên tiếp",                 longestStreak, 30),
            A("quiz-10",       "Thợ giải đề",     "Đạt 10 bài quiz",                       s.QuizPassed, 10),
            A("perfect-score", "Điểm tuyệt đối",  "Đạt 100% trong một bài kiểm tra",       s.BestScore >= 100 ? 1 : 0, 1),
            A("first-cert",    "Có bằng rồi",     "Nhận chứng chỉ đầu tiên",               s.Certificates, 1),
            A("certs-5",       "Bộ sưu tập",      "Nhận 5 chứng chỉ",                      s.Certificates, 5),
            A("active-100",    "Người bền bỉ",    "Có 100 ngày học trong năm",             s.ActiveDays, 100),
        };
    }

    private static readonly (string Id, string Label, int Min)[] Ranks =
    {
        ("bronze",   "Đồng",       0),
        ("silver",   "Bạc",        500),
        ("gold",     "Vàng",       2000),
        ("platinum", "Bạch kim",   5000),
        ("diamond",  "Kim cương",  12000),
    };

    private static RankResponse RankOf(int points)
    {
        var idx = Array.FindLastIndex(Ranks, r => points >= r.Min);
        var current = Ranks[Math.Max(0, idx)];
        var next = idx + 1 < Ranks.Length ? Ranks[idx + 1] : ((string, string, int)?)null;

        return new RankResponse
        {
            Id = current.Id,
            Label = current.Label,
            MinPoints = current.Min,
            NextLabel = next?.Item2,
            NextMinPoints = next?.Item3,
        };
    }

    private static double Round(double v) => Math.Round(v, 1);
}
