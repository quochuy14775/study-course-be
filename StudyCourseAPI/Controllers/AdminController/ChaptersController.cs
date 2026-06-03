using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.AdminController
{
    [Route("api/Courses/{courseId}/[controller]")]
    [Authorize]
    public class ChaptersController : BaseController<Chapter>
    {
        private readonly IRepository<Chapter> _chapterRepository;
        private readonly IRepository<Course> _courseRepository;

        public ChaptersController(
            IRepository<Chapter> baseRepository,
            ICurrentUser currentUser,
            IRepository<Chapter> chapterRepository,
            IRepository<Course> courseRepository)
            : base(baseRepository, currentUser)
        {
            _chapterRepository = chapterRepository;
            _courseRepository = courseRepository;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list all chapters of a course, ordered by OrderIndex
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> Get(long courseId)
        {
            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null) return NotFound();

            var chapters = await _chapterRepository.Query()
                .Where(c => c.CourseId == courseId && !c.IsDeleted)
                .Include(c => c.Lessons)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

            return Ok(chapters.Select(c => new ChapterResponse(c)));
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long courseId, long id)
        {
            var chapter = await _chapterRepository.Query()
                .Where(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted)
                .Include(c => c.Lessons)
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
                return BadRequest(new { message = "Title is required." });

            var course = await _courseRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == courseId && !c.IsDeleted);

            if (course == null) return NotFound();

            var entity = new Chapter
            {
                Title = model.Title.Trim(),
                Description = model.Description?.Trim(),
                OrderIndex = model.OrderIndex,
                CourseId = courseId,
                IsActive = model.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _chapterRepository.Add(entity);
            await _chapterRepository.SaveChangesAsync();

            var created = await _chapterRepository.Query()
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
                return BadRequest(new { message = "Title is required." });

            var entity = await _chapterRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

            if (entity == null) return NotFound();

            entity.Title = model.Title.Trim();
            entity.Description = model.Description?.Trim();
            entity.OrderIndex = model.OrderIndex;
            entity.IsActive = model.IsActive;
            entity.UpdatedAt = DateTime.UtcNow;

            await _chapterRepository.SaveChangesAsync();

            var updated = await _chapterRepository.Query()
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
            var entity = await _chapterRepository.Query()
                .FirstOrDefaultAsync(c => c.Id == id && c.CourseId == courseId && !c.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.IsActive = false;
            entity.UpdatedAt = DateTime.UtcNow;

            await _chapterRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable(long courseId, [FromBody] List<long> ids)
        {
            var entities = await _chapterRepository.Query()
                .Where(c => ids.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _chapterRepository.SaveChangesAsync();
            return NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // PUT /enable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("enable")]
        public async Task<IActionResult> Enable(long courseId, [FromBody] List<long> ids)
        {
            var entities = await _chapterRepository.Query()
                .Where(c => ids.Contains(c.Id) && c.CourseId == courseId && !c.IsDeleted)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsActive = true;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _chapterRepository.SaveChangesAsync();
            return NoContent();
        }
    }
}
