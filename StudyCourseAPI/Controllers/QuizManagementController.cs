using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

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
    public class LessonQuizManagementController : BaseController<Quiz>
    {
        private readonly IRepository<Lesson> _lessonRepository;

        public LessonQuizManagementController(
            IRepository<Quiz> baseRepository,
            IRepository<Lesson> lessonRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _lessonRepository = lessonRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(long lessonId)
        {
            var quiz = await _baseRepository.Query()
                .AsNoTracking()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);

            if (quiz == null) return NotFound();
            return Ok(new QuizResponse(quiz));
        }

        [HttpPut]
        public async Task<IActionResult> Upsert(long lessonId, [FromBody] QuizRequest model)
        {
            var lesson = await _lessonRepository.Query()
                .FirstOrDefaultAsync(l => l.Id == lessonId && !l.IsDeleted);
            if (lesson == null) return NotFound(new { message = "Lesson not found." });

            var (success, errors) = model.ValidateQuiz();
            if (!success)
                return this.ValidationFailed(errors);

            var entity = await _baseRepository.Query()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);

            if (entity == null)
            {
                entity = model.ToEntity(QuizType.Lesson, lesson.CourseId, lessonId);
                _baseRepository.Add(entity);
            }
            else
            {
                model.MapTo(entity);
            }

            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true, message = "Lesson quiz saved.", data = new QuizResponse(entity) });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(long lessonId)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(q => q.LessonId == lessonId && q.QuizType == QuizType.Lesson && !q.IsDeleted);
            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }

    /// <summary>Admin authoring for the course-level final test: GET/PUT/DELETE api/admin/courses/{courseId}/test.</summary>
    [Route("api/admin/courses/{courseId:long}/test")]
    [Authorize(Roles = AppRoles.Admin)]
    public class CourseTestManagementController : BaseController<Quiz>
    {
        private readonly IRepository<Course> _courseRepository;

        public CourseTestManagementController(
            IRepository<Quiz> baseRepository,
            IRepository<Course> courseRepository,
            ICurrentUser currentUser)
            : base(baseRepository, currentUser)
        {
            _courseRepository = courseRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get(long courseId)
        {
            var quiz = await _baseRepository.Query()
                .AsNoTracking()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);

            if (quiz == null) return NotFound();
            return Ok(new QuizResponse(quiz));
        }

        [HttpPut]
        public async Task<IActionResult> Upsert(long courseId, [FromBody] QuizRequest model)
        {
            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return NotFound(new { message = "Course not found." });

            var (success, errors) = model.ValidateQuiz();
            if (!success)
                return this.ValidationFailed(errors);

            var entity = await _baseRepository.Query()
                .Include(q => q.Questions)
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);

            if (entity == null)
            {
                entity = model.ToEntity(QuizType.CourseTest, courseId, null);
                _baseRepository.Add(entity);
            }
            else
            {
                model.MapTo(entity);
            }

            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true, message = "Course test saved.", data = new QuizResponse(entity) });
        }

        [HttpDelete]
        public async Task<IActionResult> Delete(long courseId)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(q => q.CourseId == courseId && q.QuizType == QuizType.CourseTest && !q.IsDeleted);
            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }
    }
}
