using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    /// <summary>
    /// Admin authoring for lesson quizzes: GET/PUT/DELETE api/admin/lessons/{lessonId}/quiz.
    /// PUT is an upsert — creates the quiz if none exists yet, otherwise replaces its
    /// question/option set wholesale (quiz content is small and admin-authored, so a
    /// full replace is simpler than diffing individual questions).
    /// </summary>
    [Route("api/admin/lessons/{lessonId:long}/quiz")]
    [Authorize(Roles = AppRoles.Admin)]
    public class LessonQuizManagementController : ControllerBase
    {
        private readonly IQuizManagementService _quizManagementService;

        public LessonQuizManagementController(IQuizManagementService quizManagementService)
        {
            _quizManagementService = quizManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(long lessonId)
        {
            var quiz = await _quizManagementService.GetLessonQuizAsync(lessonId);

            if (quiz == null) return NotFound();
            return Ok(quiz);
        }

        [HttpPut]
        public async Task<IActionResult> Upsert(long lessonId, [FromBody] QuizRequest model)
        {
            var result = await _quizManagementService.UpsertLessonQuizAsync(lessonId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return Ok(new { success = true, message = "Lesson quiz saved.", data = result.Data });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(long lessonId)
        {
            var deleted = await _quizManagementService.DeleteLessonQuizAsync(lessonId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }

    /// <summary>Admin authoring for the course-level final test: GET/PUT/DELETE api/admin/courses/{courseId}/test.</summary>
    [Route("api/admin/courses/{courseId:long}/test")]
    [Authorize(Roles = AppRoles.Admin)]
    public class CourseTestManagementController : ControllerBase
    {
        private readonly IQuizManagementService _quizManagementService;

        public CourseTestManagementController(IQuizManagementService quizManagementService)
        {
            _quizManagementService = quizManagementService;
        }

        [HttpGet]
        public async Task<IActionResult> Get(long courseId)
        {
            var quiz = await _quizManagementService.GetCourseTestAsync(courseId);

            if (quiz == null) return NotFound();
            return Ok(quiz);
        }

        [HttpPut]
        public async Task<IActionResult> Upsert(long courseId, [FromBody] QuizRequest model)
        {
            var result = await _quizManagementService.UpsertCourseTestAsync(courseId, model);

            if (result.IsNotFound) return NotFound(new { message = result.NotFoundMessage });
            if (!result.IsSuccess) return this.ValidationFailed(result.Errors);

            return Ok(new { success = true, message = "Course test saved.", data = result.Data });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(long courseId)
        {
            var deleted = await _quizManagementService.DeleteCourseTestAsync(courseId);

            if (!deleted) return NotFound();
            return Ok(new { success = true });
        }
    }
}
