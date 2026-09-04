using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers
{
    /// <summary>Learner-facing quiz taking: fetch (answers hidden), submit, and read own attempt history.</summary>
    [Route("api")]
    [ApiController]
    [Authorize]
    public class QuizController : BaseController<Quiz>
    {
        private readonly IRepository<Lesson> _lessonRepository;
        private readonly IRepository<UserLessonProgress> _progressRepository;
        private readonly IRepository<QuizAttempt> _attemptRepository;
        private readonly IRepository<Certificate> _certificateRepository;
        private readonly IRepository<UserCourse> _userCourseRepository;

        public QuizController(
            IRepository<Quiz> baseRepository,
            IRepository<Lesson> lessonRepository,
            IRepository<UserLessonProgress> progressRepository,
            IRepository<QuizAttempt> attemptRepository,
            IRepository<Certificate> certificateRepository,
            IRepository<UserCourse> userCourseRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _lessonRepository = lessonRepository;
            _progressRepository = progressRepository;
            _attemptRepository = attemptRepository;
            _certificateRepository = certificateRepository;
            _userCourseRepository = userCourseRepository;
        }

        // ─────────────────────────────────────────────────────────
        // GET api/lessons/{lessonId}/quiz — quiz to take (answers hidden)
        // ─────────────────────────────────────────────────────────
        [HttpGet("lessons/{lessonId:long}/quiz")]
        public async Task<IActionResult> GetLessonQuiz(long lessonId)
        {
            var quiz = await _baseRepository.Query()
                .AsNoTracking()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);

            if (quiz == null) return NotFound();
            return Ok(new QuizForAttemptResponse(quiz));
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/test — course-test card: meta + eligibility + last attempt
        // ─────────────────────────────────────────────────────────
        [HttpGet("courses/{courseId:long}/test")]
        public async Task<IActionResult> GetCourseTest(long courseId)
        {
            var quiz = await _baseRepository.Query()
                .AsNoTracking()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);

            if (quiz == null) return NotFound();

            var userId = _currentUser.GetCurrentUserId();
            var unlocked = await IsCourseTestUnlocked(courseId, userId);

            var lastAttempt = await _attemptRepository.Query()
                .AsNoTracking()
                .Where(a => a.QuizId == quiz.Id && a.UserId == userId)
                .OrderByDescending(a => a.SubmittedAt)
                .FirstOrDefaultAsync();

            return Ok(new CourseTestResponse
            {
                Id = quiz.Id,
                Title = quiz.Title,
                QuestionCount = quiz.Questions.Count,
                TimeLimitMinutes = quiz.TimeLimitMinutes,
                PassPercentage = quiz.PassPercentage,
                Unlocked = unlocked,
                LastAttempt = lastAttempt == null ? null : new QuizAttemptResultResponse(lastAttempt),
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/test/take — full quiz (answers hidden) to take the test
        // ─────────────────────────────────────────────────────────
        [HttpGet("courses/{courseId:long}/test/take")]
        public async Task<IActionResult> GetCourseTestToTake(long courseId)
        {
            var quiz = await _baseRepository.Query()
                .AsNoTracking()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);

            if (quiz == null) return NotFound();

            var userId = _currentUser.GetCurrentUserId();
            var unlocked = await IsCourseTestUnlocked(courseId, userId);
            if (!unlocked)
                return BadRequest(new { status = 400, message = "Course test is locked until all lessons and lesson quizzes are passed." });

            return Ok(new QuizForAttemptResponse(quiz));
        }

        // ─────────────────────────────────────────────────────────
        // POST api/quizzes/{quizId}/attempts — submit + grade
        // ─────────────────────────────────────────────────────────
        [HttpPost("quizzes/{quizId:long}/attempts")]
        public async Task<IActionResult> Submit(long quizId, [FromBody] SubmitQuizAttemptRequest model)
        {
            var quiz = await _baseRepository.Query()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.Id == quizId && !q.IsDeleted);
            if (quiz == null) return NotFound();

            // Course tests stay locked server-side too — a determined client could otherwise
            // hit this endpoint directly and skip the "finish everything first" requirement.
            if (quiz.QuizType == QuizType.CourseTest)
            {
                var userIdForGate = _currentUser.GetCurrentUserId();
                var unlocked = await IsCourseTestUnlocked(quiz.CourseId, userIdForGate);
                if (!unlocked)
                    return BadRequest(new { status = 400, message = "Course test is locked until all lessons and lesson quizzes are passed." });
            }

            var userId = _currentUser.GetCurrentUserId();
            var attemptCount = await _attemptRepository.Query()
                .Where(a => a.QuizId == quizId && a.UserId == userId)
                .CountAsync();

            var attempt = quiz.Grade(model, userId, attemptCount + 1);

            _attemptRepository.Add(attempt);
            await _attemptRepository.SaveChangesAsync();

            // Passing the course test is what completes the course — issue the certificate right here
            // so the two facts (test passed / certificate exists) can never drift out of sync.
            if (quiz.QuizType == QuizType.CourseTest && attempt.IsPassed)
            {
                await _certificateRepository.IssueIfEligibleAsync(_userCourseRepository, quiz.CourseId, userId, attempt.PercentageScore);
                await _certificateRepository.SaveChangesAsync();
                await _userCourseRepository.SaveChangesAsync();
            }

            return Ok(new QuizAttemptResultResponse(attempt));
        }

        // ─────────────────────────────────────────────────────────
        // GET api/quizzes/{quizId}/attempts — own attempt history
        // ─────────────────────────────────────────────────────────
        [HttpGet("quizzes/{quizId:long}/attempts")]
        public async Task<IActionResult> GetAttempts(long quizId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var attempts = await _attemptRepository.Query()
                .AsNoTracking()
                .Where(a => a.QuizId == quizId && a.UserId == userId)
                .OrderByDescending(a => a.AttemptNumber)
                .ToListAsync();

            return Ok(attempts.Select(a => new QuizAttemptResultResponse(a)));
        }

        // ─────────────────────────────────────────────────────────
        // helpers
        // ─────────────────────────────────────────────────────────
        private async Task<bool> IsCourseTestUnlocked(long courseId, long userId)
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

            var lessonQuizzes = await _baseRepository.Query()
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
}
