using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class QuizAttemptService : IQuizAttemptService
{
    private const string LockedMessage =
        "Course test is locked until all lessons and lesson quizzes are passed.";

    private readonly IRepository<Quiz> _quizRepository;
    private readonly IRepository<Lesson> _lessonRepository;
    private readonly IRepository<UserLessonProgress> _progressRepository;
    private readonly IRepository<QuizAttempt> _attemptRepository;
    private readonly IRepository<Certificate> _certificateRepository;
    private readonly IRepository<UserCourse> _userCourseRepository;
    private readonly ICurrentUser _currentUser;

    public QuizAttemptService(
        IRepository<Quiz> quizRepository,
        IRepository<Lesson> lessonRepository,
        IRepository<UserLessonProgress> progressRepository,
        IRepository<QuizAttempt> attemptRepository,
        IRepository<Certificate> certificateRepository,
        IRepository<UserCourse> userCourseRepository,
        ICurrentUser currentUser)
    {
        _quizRepository = quizRepository;
        _lessonRepository = lessonRepository;
        _progressRepository = progressRepository;
        _attemptRepository = attemptRepository;
        _certificateRepository = certificateRepository;
        _userCourseRepository = userCourseRepository;
        _currentUser = currentUser;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<QuizForAttemptResponse?> GetLessonQuizAsync(long lessonId)
    {
        var quiz = await _quizRepository.Query()
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q =>
                q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);

        return quiz == null ? null : new QuizForAttemptResponse(quiz);
    }

    public async Task<CourseTestResponse?> GetCourseTestAsync(long courseId)
    {
        var quiz = await FindCourseTestAsync(courseId);
        if (quiz == null) return null;

        var userId = _currentUser.GetCurrentUserId();
        var unlocked = await IsCourseTestUnlockedAsync(courseId, userId);

        var lastAttempt = await _attemptRepository.Query()
            .AsNoTracking()
            .Where(a => a.QuizId == quiz.Id && a.UserId == userId)
            .OrderByDescending(a => a.SubmittedAt)
            .FirstOrDefaultAsync();

        return new CourseTestResponse
        {
            Id = quiz.Id,
            Title = quiz.Title,
            QuestionCount = quiz.Questions.Count,
            TimeLimitMinutes = quiz.TimeLimitMinutes,
            PassPercentage = quiz.PassPercentage,
            Unlocked = unlocked,
            LastAttempt = lastAttempt == null ? null : new QuizAttemptResultResponse(lastAttempt),
        };
    }

    public async Task<ServiceResult<QuizForAttemptResponse>> GetCourseTestToTakeAsync(long courseId)
    {
        var quiz = await FindCourseTestAsync(courseId);
        if (quiz == null)
            return ServiceResult<QuizForAttemptResponse>.NotFound();

        var userId = _currentUser.GetCurrentUserId();
        var unlocked = await IsCourseTestUnlockedAsync(courseId, userId);

        if (!unlocked)
            return ServiceResult<QuizForAttemptResponse>.Invalid(LockedMessage);

        return ServiceResult<QuizForAttemptResponse>.Ok(new QuizForAttemptResponse(quiz));
    }

    public async Task<List<QuizAttemptResultResponse>> GetOwnAttemptsAsync(long quizId)
    {
        var userId = _currentUser.GetCurrentUserId();

        // Materialize trước rồi mới map: QuizAttemptResultResponse có logic trong constructor,
        // EF không dịch được thành SQL nếu đặt trong .Select() của IQueryable.
        var attempts = await _attemptRepository.Query()
            .AsNoTracking()
            .Where(a => a.QuizId == quizId && a.UserId == userId)
            .OrderByDescending(a => a.AttemptNumber)
            .ToListAsync();

        return attempts.Select(a => new QuizAttemptResultResponse(a)).ToList();
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<QuizAttemptResultResponse>> SubmitAsync(
        long quizId, SubmitQuizAttemptRequest model)
    {
        var quiz = await _quizRepository.Query()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);

        if (quiz == null)
            return ServiceResult<QuizAttemptResultResponse>.NotFound();

        var userId = _currentUser.GetCurrentUserId();

        // Course test cũng bị khoá ở phía server — nếu chỉ khoá ở UI thì client cứng đầu
        // vẫn gọi thẳng endpoint này để bỏ qua yêu cầu "học xong hết đã".
        if (quiz.QuizType == QuizType.CourseTest)
        {
            var unlocked = await IsCourseTestUnlockedAsync(quiz.CourseId, userId);
            if (!unlocked)
                return ServiceResult<QuizAttemptResultResponse>.Invalid(LockedMessage);
        }

        var attemptCount = await _attemptRepository.Query()
            .Where(a => a.QuizId == quizId && a.UserId == userId)
            .CountAsync();

        var attempt = quiz.Grade(model, userId, attemptCount + 1);

        _attemptRepository.Add(attempt);
        await _attemptRepository.SaveChangesAsync();

        // Pass course test chính là điều kiện hoàn thành khoá học — cấp certificate ngay tại đây
        // để hai sự kiện (pass bài test / có certificate) không bao giờ lệch nhau.
        if (quiz.QuizType == QuizType.CourseTest && attempt.IsPassed)
        {
            await _certificateRepository.IssueIfEligibleAsync(
                _userCourseRepository, quiz.CourseId, userId, attempt.PercentageScore);

            await _certificateRepository.SaveChangesAsync();
            await _userCourseRepository.SaveChangesAsync();
        }

        return ServiceResult<QuizAttemptResultResponse>.Ok(new QuizAttemptResultResponse(attempt));
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private Task<Quiz?> FindCourseTestAsync(long courseId)
        => _quizRepository.Query()
            .AsNoTracking()
            .Include(q => q.Questions)
            .FirstOrDefaultAsync(q =>
                q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);

    /// <summary>
    /// Course test chỉ mở khi user đã hoàn thành TẤT CẢ lesson và pass TẤT CẢ quiz của lesson.
    /// </summary>
    private async Task<bool> IsCourseTestUnlockedAsync(long courseId, long userId)
    {
        var lessons = await _lessonRepository.Query()
            .AsNoTracking()
            .Where(l => l.CourseId == courseId && !l.IsDeleted)
            .Select(l => l.Id)
            .ToListAsync();

        if (lessons.Count == 0) return false;

        var completedLessonIds = await _progressRepository.Query()
            .AsNoTracking()
            .Where(p => p.UserId == userId && lessons.Contains(p.LessonId) && p.IsCompleted)
            .Select(p => p.LessonId)
            .ToListAsync();

        if (completedLessonIds.Count < lessons.Count) return false;

        var lessonQuizzes = await _quizRepository.Query()
            .AsNoTracking()
            .Where(q => q.QuizType == QuizType.Lesson && lessons.Contains(q.LessonId!.Value) && !q.IsDeleted)
            .Select(q => new { q.Id, q.LessonId })
            .ToListAsync();

        if (lessonQuizzes.Count == 0) return true;

        var quizIds = lessonQuizzes.Select(q => q.Id).ToList();

        var passedQuizIds = await _attemptRepository.Query()
            .AsNoTracking()
            .Where(a => a.UserId == userId && quizIds.Contains(a.QuizId) && a.IsPassed)
            .Select(a => a.QuizId)
            .Distinct()
            .ToListAsync();

        return lessonQuizzes.All(q => passedQuizIds.Contains(q.Id));
    }
}
