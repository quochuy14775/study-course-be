using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    /// <summary>Persists per-user lesson completion — the source of truth QuizController's course-test gate reads from.</summary>
    [ApiController]
    [Authorize]
    public class LessonProgressController : ControllerBase
    {
        private readonly ILessonProgressService _progressService;

        public LessonProgressController(ILessonProgressService progressService)
        {
            _progressService = progressService;
        }

        // ─────────────────────────────────────────────────────────
        // POST api/lessons/{lessonId}/progress/complete — idempotent upsert
        // ─────────────────────────────────────────────────────────
        [HttpPost("api/lessons/{lessonId:long}/progress/complete")]
        public async Task<IActionResult> MarkComplete(long lessonId)
        {
            var marked = await _progressService.MarkCompleteAsync(lessonId);

            if (!marked) return NotFound();
            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/progress — completed lesson ids for the current user
        // (hydrates FE state on page load — otherwise "done" state resets on every refresh)
        // ─────────────────────────────────────────────────────────
        [HttpGet("api/courses/{courseId:long}/progress")]
        public async Task<IActionResult> GetCourseProgress(long courseId)
        {
            return Ok(await _progressService.GetCompletedLessonIdsAsync(courseId));
        }
    }
}
