using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers
{
    [Route("api/Courses/{courseId}/[controller]")]
    [Authorize]
    public class ChaptersController : BaseController<Chapter>
    {
        private readonly IRepository<Course> _courseRepository;

        public ChaptersController(
            IRepository<Chapter> baseRepository,
            ICurrentUser currentUser,
            IRepository<Course> courseRepository)
            : base(baseRepository, currentUser)
        {
            _courseRepository = courseRepository;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list all chapters of a course, ordered by OrderIndex
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Get(long courseId)
        {
            // Combined check + fetch: if course doesn't exist, chapters list will be empty.
            // Use a single round-trip with AnyAsync for course existence check IF empty result.
            var chapters = await _baseRepository.Query()
                .AsNoTracking()
                .Where(c => c.CourseId == courseId && !c.IsDeleted)
                .Include(c => c.Lessons.Where(l => !l.IsDeleted))
                .AsSplitQuery()
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            // Only hit DB for course-existence if no chapters (cheap recovery, common case has data)
            if (chapters.Count == 0)
            {
                var courseExists = await _courseRepository.Query()
                    .AsNoTracking()
                    .AnyAsync(c => c.Id == courseId && !c.IsDeleted);
                if (!courseExists) return NotFound();
            }

            return Ok(chapters.Select(c => new ChapterResponse(c)));
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long courseId, long id)
        {
            var chapter = await _baseRepository.Query()
                .AsNoTracking()
                .Where(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted)
                .Include(c => c.Lessons.Where(l => !l.IsDeleted))
                .AsSplitQuery()
                .FirstOrDefaultAsync();

            if (chapter == null) return NotFound();
            return Ok(new ChapterResponse(chapter));
        }

        // ─────────────────────────────────────────────────────────
        // POST — create a new chapter for the course
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post(long courseId, [FromBody] ChapterRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
                return BadRequest(new { status = 400, message = "Title is required." });

            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null) return NotFound();

            var entity = new Chapter
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                OrderIndex = model.OrderIndex,
                CourseId = courseId,
                IsActive = model.IsActive
            };

            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            var created = await _baseRepository.Query()
                .Where(c => c.Id == entity.Id)
                .Include(c => c.Lessons)
                .FirstAsync();

            return CreatedAtAction(nameof(Get), new { courseId, id = entity.Id }, new ChapterResponse(created));
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long courseId, long id, [FromBody] ChapterRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Title))
                return BadRequest(new { status = 400, message = "Title is required." });

            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

            if (entity == null) return NotFound();

            entity.Title = model.Title.Trim();
            entity.Description = model.Description?.Trim();
            entity.OrderIndex = model.OrderIndex;
            entity.IsActive = model.IsActive;

            await _baseRepository.SaveChangesAsync();

            var updated = await _baseRepository.Query()
                .Where(c => c.Id == entity.Id)
                .Include(c => c.Lessons)
                .FirstAsync();

            return Ok(new ChapterResponse(updated));
        }

        // ─────────────────────────────────────────────────────────
        // DELETE {id} — soft delete
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long courseId, long id)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.IsActive = false;

            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable(long courseId, [FromBody] List<long> ids)
        {
            var now = DateTime.UtcNow;
            var affected = await _baseRepository.Query()
                .Where(c => ids.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsActive, false)
                    .SetProperty(c => c.UpdatedAt, now));

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable(long courseId, [FromBody] List<long> ids)
        {
            var now = DateTime.UtcNow;
            var affected = await _baseRepository.Query()
                .Where(c => ids.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsActive, true)
                    .SetProperty(c => c.UpdatedAt, now));

            return affected == 0 ? NotFound() : NoContent();
        }
    }
}
