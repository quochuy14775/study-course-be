using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers
{
    /// <summary>Persists per-user lesson completion — the source of truth QuizController's course-test gate reads from.</summary>
    [ApiController]
    [Authorize]
    public class LessonProgressController : BaseController<UserLessonProgress>
    {
        private readonly IRepository<Lesson> _lessonRepository;

        public LessonProgressController(
            IRepository<UserLessonProgress> baseRepository,
            IRepository<Lesson> lessonRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _lessonRepository = lessonRepository;
        }

        // ─────────────────────────────────────────────────────────
        // POST api/lessons/{lessonId}/progress/complete — idempotent upsert
        // ─────────────────────────────────────────────────────────
        [HttpPost("api/lessons/{lessonId:long}/progress/complete")]
        public async Task<IActionResult> MarkComplete(long lessonId)
        {
            var lesson = await _lessonRepository.Query()
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (lesson == null) return NotFound();

            var userId = _currentUser.GetCurrentUserId();
            var now = DateTime.UtcNow;

            var progress = await _baseRepository.Query()
                .FirstOrDefaultAsync(p => p.LessonId == lessonId && p.UserId == userId);

            if (progress == null)
            {
                progress = new UserLessonProgress
                {
                    UserId = userId,
                    LessonId = lessonId,
                    CourseId = lesson.CourseId,
                    IsCompleted = true,
                    CompletedAt = now,
                    LastWatchedAt = now,
                    IsActive = true,
                };
                _baseRepository.Add(progress);
            }
            else if (!progress.IsCompleted)
            {
                progress.IsCompleted = true;
                progress.CompletedAt = now;
                progress.LastWatchedAt = now;
            }

            await _baseRepository.SaveChangesAsync();
            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/progress — completed lesson ids for the current user
        // (hydrates FE state on page load — otherwise "done" state resets on every refresh)
        // ─────────────────────────────────────────────────────────
        [HttpGet("api/courses/{courseId:long}/progress")]
        public async Task<IActionResult> GetCourseProgress(long courseId)
        {
            var userId = _currentUser.GetCurrentUserId();

            var completedLessonIds = await _baseRepository.Query()
                .AsNoTracking()
                .Where(p => p.UserId == userId && p.CourseId == courseId && p.IsCompleted)
                .Select(p => p.LessonId)
                .ToListAsync();

            return Ok(completedLessonIds);
        }
    }
}
