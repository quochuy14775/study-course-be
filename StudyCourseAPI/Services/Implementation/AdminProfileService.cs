using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Data;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

/// <summary>
/// Trang cá nhân admin. Đóng góp gắn với admin qua CreatedBy/UpdatedBy (email, do DbContext ghi)
/// ở Course/Article/Roadmap và UserId ở QuestionAnswers / CourseReviewReplies / LessonComments.
/// Chỉ đọc; dùng thẳng DbContext như các service tổng hợp khác.
/// </summary>
public class AdminProfileService : IAdminProfileService
{
    private readonly ApplicationDbContext _db;
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminProfileService(ApplicationDbContext db, ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<AdminProfileResponse?> GetMyProfileAsync()
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return null;
        var userId = user.Id;
        var email = user.Email ?? string.Empty;
        var roles = await _userManager.GetRolesAsync(user);

        // ── Khóa học của tôi ──
        var myCourses = await _db.Courses
            .AsNoTracking()
            .Where(c => c.CreatedBy == email)
            .OrderByDescending(c => c.CreatedAt)
            .Select(c => new AdminOwnedCourseResponse
            {
                Id = c.Id, Title = c.Title, ImageUrl = c.ImageUrl, Level = c.Level.ToString(),
                IsActive = c.IsActive, Price = c.Price, LessonCount = c.LessonCount, Rating = c.Rating,
                Learners = c.UserCourses.Count(), CreatedAt = c.CreatedAt, UpdatedAt = c.UpdatedAt,
            })
            .ToListAsync();

        var contribution = new AdminContributionResponse
        {
            CoursesCreated = myCourses.Count,
            CoursesUpdated = await _db.Courses.CountAsync(c => c.UpdatedBy == email),
            LessonsInMyCourses = myCourses.Sum(c => c.LessonCount),
            LearnersInMyCourses = myCourses.Sum(c => c.Learners),
            ArticlesWritten = await _db.Articles.CountAsync(a => a.CreatedBy == email),
            AnswersGiven = await _db.QuestionAnswers.CountAsync(a => a.UserId == userId),
            ReviewRepliesGiven = await _db.CourseReviewReplies.CountAsync(r => r.UserId == userId),
        };

        // ── Hệ thống ──
        var system = new AdminSystemSnapshotResponse
        {
            Courses = await _db.Courses.CountAsync(),
            ActiveCourses = await _db.Courses.CountAsync(c => c.IsActive),
            Learners = await _db.Users.CountAsync(u => !u.IsDeleted),
            Enrollments = await _db.UserCourses.CountAsync(),
            CertificatesIssued = await _db.Certificates.CountAsync(),
            PendingQuestions = await _db.LessonQuestions.CountAsync(q => !q.IsResolved && q.AnswerCount == 0),
            PendingLowReviews = await _db.CourseReviews.CountAsync(r => r.Rating <= 2 && !r.Replies.Any()),
            CoursesWithoutTest = await _db.Courses.CountAsync(c => c.IsActive && c.LessonCount > 0
                && !_db.Quizzes.Any(q => q.CourseId == c.Id && q.QuizType == QuizType.CourseTest)),
        };

        return new AdminProfileResponse
        {
            Profile = UserProfileResponse.UserProfile(user, roles),
            JoinedAt = user.CreatedAt,
            Contribution = contribution,
            System = system,
            MyCourses = myCourses.Take(6).ToList(),
            Recent = await RecentAsync(userId, email),
            Activity = await ActivityAsync(userId, email, days: 365),
        };
    }

    // ─────────────────────────────────────────────────────────

    private async Task<List<AdminRecentActionResponse>> RecentAsync(long userId, string email)
    {
        const int take = 8;

        var created = await _db.Courses.AsNoTracking()
            .Where(c => c.CreatedBy == email)
            .OrderByDescending(c => c.CreatedAt).Take(take)
            .Select(c => new AdminRecentActionResponse { Kind = "course_created", Title = c.Title, CourseId = c.Id, At = c.CreatedAt })
            .ToListAsync();

        var updated = await _db.Courses.AsNoTracking()
            .Where(c => c.UpdatedBy == email && c.UpdatedAt != null)
            .OrderByDescending(c => c.UpdatedAt).Take(take)
            .Select(c => new AdminRecentActionResponse { Kind = "course_updated", Title = c.Title, CourseId = c.Id, At = c.UpdatedAt!.Value })
            .ToListAsync();

        var articles = await _db.Articles.AsNoTracking()
            .Where(a => a.CreatedBy == email)
            .OrderByDescending(a => a.CreatedAt).Take(take)
            .Select(a => new AdminRecentActionResponse { Kind = "article", Title = a.Title, ArticleId = a.Id, At = a.CreatedAt })
            .ToListAsync();

        var answers = await _db.QuestionAnswers.AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt).Take(take)
            .Select(a => new AdminRecentActionResponse
            {
                Kind = "answer", Title = a.Question.Lesson.Title, CourseId = a.Question.Lesson.CourseId,
                LessonId = a.Question.LessonId, At = a.CreatedAt,
            })
            .ToListAsync();

        var replies = await _db.CourseReviewReplies.AsNoTracking()
            .Where(r => r.UserId == userId)
            .OrderByDescending(r => r.CreatedAt).Take(take)
            .Select(r => new AdminRecentActionResponse { Kind = "reply", Title = r.Review.Course.Title, CourseId = r.Review.CourseId, At = r.CreatedAt })
            .ToListAsync();

        return created.Concat(updated).Concat(articles).Concat(answers).Concat(replies)
            .OrderByDescending(x => x.At)
            .Take(take)
            .ToList();
    }

    /// <summary>
    /// Hoạt động quản trị theo ngày. Map vào shape hoạt động học tập để FE dùng lại contribution graph:
    /// Lessons = khóa tạo/sửa · Quizzes = bài viết · Posts = trả lời/phản hồi/bình luận · Enrollments = roadmap.
    /// </summary>
    private async Task<UserActivityResponse> ActivityAsync(long userId, string email, int days)
    {
        var todayLocal = DateOnly.FromDateTime(DateTime.UtcNow + ActivityCalendar.TzOffset);
        var startLocal = todayLocal.AddDays(1 - days);
        var from = ActivityCalendar.ToUtc(startLocal);
        var to = ActivityCalendar.ToUtc(todayLocal.AddDays(1));

        const string sql = """
            SELECT (e.ts AT TIME ZONE 'Asia/Ho_Chi_Minh')::date AS "Day", e.kind AS "Kind", COUNT(*)::int AS "Count"
            FROM (
                SELECT "CreatedAt" AS ts, 'course'  AS kind FROM "Courses"  WHERE "CreatedBy" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "UpdatedAt", 'course'  FROM "Courses"             WHERE "UpdatedBy" = {0} AND "UpdatedAt" IS NOT NULL AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt", 'article' FROM "Articles"            WHERE "CreatedBy" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "UpdatedAt", 'article' FROM "Articles"            WHERE "UpdatedBy" = {0} AND "UpdatedAt" IS NOT NULL AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt", 'roadmap' FROM "Roadmaps"            WHERE "CreatedBy" = {0} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt", 'post'    FROM "QuestionAnswers"     WHERE "UserId" = {1} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt", 'post'    FROM "CourseReviewReplies" WHERE "UserId" = {1} AND "IsDeleted" = false
                UNION ALL SELECT "CreatedAt", 'post'    FROM "LessonComments"      WHERE "UserId" = {1} AND "IsDeleted" = false
            ) e
            WHERE e.ts >= {2} AND e.ts < {3}
            GROUP BY 1, 2
            """;

        var rows = await _db.Database.SqlQueryRaw<ActivityRow>(sql, email, userId, from, to).ToListAsync();
        var byDay = ActivityCalendar.EmptyDays(startLocal, days);

        foreach (var r in rows)
        {
            if (!byDay.TryGetValue(r.Day, out var day)) continue;
            switch (r.Kind)
            {
                case "course":  day.Lessons += r.Count; break;
                case "article": day.Quizzes += r.Count; break;
                case "post":    day.Posts += r.Count; break;
                case "roadmap": day.Enrollments += r.Count; break;
            }
            day.Count += r.Count;
        }

        return ActivityCalendar.Build(byDay, todayLocal, startLocal, days);
    }
}
