using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    /// <summary>Learner-facing quiz taking: fetch (answers hidden), submit, and read own attempt history.</summary>
    [Route("api")]
    [ApiController]
    [Authorize]
    public class QuizController : ControllerBase
    {
        private readonly IQuizAttemptService _quizAttemptService;

        public QuizController(IQuizAttemptService quizAttemptService)
        {
            _quizAttemptService = quizAttemptService;
        }

        // ─────────────────────────────────────────────────────────
        // GET api/lessons/{lessonId}/quiz — quiz to take (answers hidden)
        // ─────────────────────────────────────────────────────────
        [HttpGet("lessons/{lessonId:long}/quiz")]
        public async Task<IActionResult> GetLessonQuiz(long lessonId)
        {
            var quiz = await _quizAttemptService.GetLessonQuizAsync(lessonId);

            if (quiz == null) return NotFound();
            return Ok(quiz);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/test — course-test card: meta + eligibility + last attempt
        // ─────────────────────────────────────────────────────────
        [HttpGet("courses/{courseId:long}/test")]
        public async Task<IActionResult> GetCourseTest(long courseId)
        {
            var test = await _quizAttemptService.GetCourseTestAsync(courseId);

            if (test == null) return NotFound();
            return Ok(test);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/courses/{courseId}/test/take — full quiz (answers hidden) to take the test
        // ─────────────────────────────────────────────────────────
        [HttpGet("courses/{courseId:long}/test/take")]
        public async Task<IActionResult> GetCourseTestToTake(long courseId)
        {
            var result = await _quizAttemptService.GetCourseTestToTakeAsync(courseId);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────
        // POST api/quizzes/{quizId}/attempts — submit + grade
        // ─────────────────────────────────────────────────────────
        [HttpPost("quizzes/{quizId:long}/attempts")]
        public async Task<IActionResult> Submit(long quizId, [FromBody] SubmitQuizAttemptRequest model)
        {
            var result = await _quizAttemptService.SubmitAsync(quizId, model);

            if (result.IsNotFound) return NotFound();
            if (!result.IsSuccess)
                return BadRequest(new { status = 400, message = result.ErrorMessage });

            return Ok(result.Data);
        }

        // ─────────────────────────────────────────────────────────
        // GET api/quizzes/{quizId}/attempts — own attempt history
        // ─────────────────────────────────────────────────────────
        [HttpGet("quizzes/{quizId:long}/attempts")]
        public async Task<IActionResult> GetAttempts(long quizId)
        {
            return Ok(await _quizAttemptService.GetOwnAttemptsAsync(quizId));
        }
    }
}
