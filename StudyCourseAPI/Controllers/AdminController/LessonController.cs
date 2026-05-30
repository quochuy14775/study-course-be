using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.AdminController
{
    [Route("api/Courses/{courseId}/[controller]")]
    [Authorize]
    public class LessonsController : BaseController<Lesson>
    {
        private readonly IRepository<Lesson> _lessonRepository;
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<Chapter> _chapterRepository;

        public LessonsController(
            IRepository<Lesson> baseRepository,
            ICurrentUser currentUser,
            IRepository<Lesson> lessonRepository,
            IRepository<Course> courseRepository,
            IRepository<Chapter> chapterRepository)
            : base(baseRepository, currentUser)
        {
            _lessonRepository = lessonRepository;
            _courseRepository = courseRepository;
            _chapterRepository = chapterRepository;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list with OData (optional ?chapterId= for filtering)
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Get(long courseId, [FromQuery] long? chapterId, ODataQueryOptions<Lesson> queryOptions)
        {
            var queryable = _lessonRepository.Query()
                .Where(x => !x.IsDeleted && x.CourseId == courseId);

            if (chapterId.HasValue)
                queryable = queryable.Where(x => x.ChapterId == chapterId.Value);

            var (count, vm) = queryable.AppendQueryOptions(queryOptions);

            var response = new ODataResponse<LessonResponse>
            {
                Count = count,
                Value = vm.Select(x => new LessonResponse(x))
            };

            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long courseId, long id)
        {
            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return NotFound();

            var lesson = await _lessonRepository.Query()
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted && l.CourseId == courseId);

            if (lesson == null) return NotFound();

            return Ok(new LessonDetailResponse(lesson));
        }

        // ─────────────────────────────────────────────────────────
        // POST — bulk create lessons within a chapter
        // Always creates a new chapter containing the lessons
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] BulkCreateLessonsRequest request)
        {
            if (request == null || request.Lessons == null || request.Lessons.Count == 0)
                return BadRequest(new { status = 400, message = "Request body must contain at least one lesson." });

            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return NotFound();

            // Determine chapter to use (optional — null = Chưa phân loại)
            Chapter? chapter = null;
            bool isNewChapter = false;

            if (request.NewChapter != null)
            {
                isNewChapter = true;
                chapter = new Chapter
                {
                    Title = request.NewChapter.Title ?? string.Empty,
                    Description = request.NewChapter.Description,
                    OrderIndex = request.NewChapter.OrderIndex,
                    CourseId = courseId,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _chapterRepository.Add(chapter);
                await _chapterRepository.SaveChangesAsync();
            }
            else if (request.ChapterId.HasValue)
            {
                chapter = await _chapterRepository.Query()
                    .FirstOrDefaultAsync(c => c.Id == request.ChapterId.Value && c.CourseId == courseId && !c.IsDeleted);

                if (chapter == null)
                    return BadRequest(new { status = 400, message = "Chapter not found." });
            }
            // else: no chapter → lessons go to uncategorized (chapterId = null)

            // Assign chapter to lessons (null = uncategorized)
            foreach (var lesson in request.Lessons)
            {
                lesson.ChapterId = chapter?.Id;
            }

            // Validate each item; collect indexed errors
            var allErrors = new Dictionary<string, object>();
            for (var i = 0; i < request.Lessons.Count; i++)
            {
                var (success, errors) = await request.Lessons[i].ValidateLessonAsync(
                    _lessonRepository, _chapterRepository, courseId);

                if (!success && errors != null)
                {
                    foreach (var kv in errors)
                    {
                        if (kv.Value == null || kv.Value.Count == 0) continue;
                        var key = $"Lessons[{i}].{kv.Key}";
                        allErrors[key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
                    }
                }
            }

            if (allErrors.Any())
                return BadRequest(new { status = 400, message = "Validation failed", errors = allErrors });

            // Build entities with safe orderIndex (auto-fill if 0 + collision)
            var entities = new List<Lesson>();
            var nextIdx = await _lessonRepository.NextOrderIndexAsync(courseId);

            foreach (var model in request.Lessons)
            {
                var entity = model.GetLesson(courseId);
                if (entity.OrderIndex <= 0) entity.OrderIndex = nextIdx++;
                else nextIdx = Math.Max(nextIdx, entity.OrderIndex + 1);
                entities.Add(entity);
                _baseRepository.Add(entity);
            }

            await _baseRepository.SaveChangesAsync();

            // Refresh cached stats on Course
            await _courseRepository.RefreshCourseStatsAsync(_lessonRepository, _chapterRepository, courseId);
            await _courseRepository.SaveChangesAsync();

            var data = entities.Select(e => new LessonResponse(e)).ToList();

            return Ok(new
            {
                success = true,
                message = $"Created {data.Count} lesson(s) successfully." + (isNewChapter ? $" Created chapter '{chapter!.Title}'." : ""),
                data,
                chapterId = chapter?.Id,
                chapterTitle = chapter?.Title ?? "Chưa phân loại"
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update single
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long courseId, long id, [FromBody] LessonRequest model)
        {
            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);
            if (course == null) return NotFound();

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted && x.CourseId == courseId);
            if (entity == null) return NotFound();


            var (success, errors) = await model.ValidateLessonAsync(
                _lessonRepository, _chapterRepository, courseId, id);

            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            model.ToEntity(entity);
            await _baseRepository.SaveChangesAsync();

            await _courseRepository.RefreshCourseStatsAsync(_lessonRepository, _chapterRepository, courseId);
            await _courseRepository.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Lesson updated successfully.",
                data = new LessonResponse(entity)
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /delete — bulk soft-delete (matches FE lessonService.deleteCourses)
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("delete")]
        public async Task<IActionResult> Delete(long courseId, [FromBody] List<long> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one lesson id." });

            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted && x.CourseId == courseId)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _baseRepository.SaveChangesAsync();

            await _courseRepository.RefreshCourseStatsAsync(_lessonRepository, _chapterRepository, courseId);
            await _courseRepository.SaveChangesAsync();

            return Ok(new { success = true, deleted = entities.Count });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable(long courseId, [FromBody] List<long> ids)
        {
            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && x.CourseId == courseId && !x.IsDeleted)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _baseRepository.SaveChangesAsync();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable(long courseId, [FromBody] List<long> ids)
        {
            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && x.CourseId == courseId && !x.IsDeleted)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsActive = true;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _baseRepository.SaveChangesAsync();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /reorder — drag-drop support (CurriculumBuilder)
        // Body: [{ id, orderIndex, chapterId? }, ...]
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("reorder")]
        public async Task<IActionResult> Reorder(long courseId, [FromBody] List<LessonReorderItem> items)
        {
            if (items == null || items.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one item." });

            var ids = items.Select(i => i.Id).ToList();
            var lessons = await _baseRepository.Query()
                .Where(l => ids.Contains(l.Id) && l.CourseId == courseId && !l.IsDeleted)
                .ToListAsync();

            if (lessons.Count != items.Count)
                return BadRequest(new { status = 400, message = "Some lessons do not belong to this course." });

            // Validate provided chapterIds (if any)
            var chapterIds = items.Where(i => i.ChapterId.HasValue).Select(i => i.ChapterId!.Value).Distinct().ToList();
            if (chapterIds.Any())
            {
                var validChapterIds = await _chapterRepository.Query()
                    .Where(c => chapterIds.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                    .Select(c => c.Id)
                    .ToListAsync();

                if (validChapterIds.Count != chapterIds.Count)
                    return BadRequest(new { status = 400, message = "Some chapters do not belong to this course." });
            }

            var byId = lessons.ToDictionary(l => l.Id);
            foreach (var item in items)
            {
                if (!byId.TryGetValue(item.Id, out var lesson)) continue;
                lesson.OrderIndex = item.OrderIndex;
                lesson.ChapterId = item.ChapterId;
                lesson.UpdatedAt = DateTime.UtcNow;
            }

            await _baseRepository.SaveChangesAsync();
            return Ok(new { success = true, updated = lessons.Count });
        }

        // ─────────────────────────────────────────────────────────
        // helpers
        // ─────────────────────────────────────────────────────────
        private static Dictionary<string, object> FlattenErrors(Dictionary<string, List<string>>? errors)
        {
            var result = new Dictionary<string, object>();
            if (errors == null) return result;

            foreach (var kv in errors)
            {
                if (kv.Value == null || kv.Value.Count == 0) continue;
                result[kv.Key] = kv.Value.Count == 1 ? kv.Value[0] : (object)kv.Value;
            }
            return result;
        }
    }
}
