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
    [Route("api/[controller]")]
    [Authorize(Roles = AppRoles.Admin)]
    public class CoursesController : BaseController<Course>
    {
        private readonly IRepository<Course> _courseRepository;
        private readonly IRepository<CourseTag> _courseTagRepository;
        private readonly IRepository<Tag> _tagRepository;

        public CoursesController(
            IRepository<Course> baseRepository,
            ICurrentUser currentUser,
            IRepository<Course> courseRepository,
            IRepository<CourseTag> courseTagRepository,
            IRepository<Tag> tagRepository)
            : base(baseRepository, currentUser)
        {
            _courseRepository = courseRepository;
            _courseTagRepository = courseTagRepository;
            _tagRepository = tagRepository;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list with OData
        // ─────────────────────────────────────────────────────────
        [HttpGet]
        public IActionResult Get(ODataQueryOptions<Course> queryOptions)
        {
            var queryable = _courseRepository.Query()
                .Where(x => !x.IsDeleted);

            var (count, vm) = queryable.AppendQueryOptions(queryOptions);

            var response = new ODataResponse<CourseResponse>
            {
                Count = count,
                Value = vm.Select(x => new CourseResponse(x))
            };

            return Ok(response);
        }

        // ─────────────────────────────────────────────────────────
        // GET single
        // ─────────────────────────────────────────────────────────
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var course = await _courseRepository.Query()
                .Include(c => c.CourseTags)
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (course == null) return NotFound();

            return Ok(new CourseDetailResponse(course));
        }

        // ─────────────────────────────────────────────────────────
        // POST
        // ─────────────────────────────────────────────────────────
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CourseRequest model)
        {
            var (success, errors) = await model.ValidateCourseAsync(_baseRepository);
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            var entity = model.GetCourse();
            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            // Sync tags after course id is available
            if (model.TagIds != null && model.TagIds.Any())
            {
                await entity.SyncTagsAsync(_courseTagRepository, _tagRepository, model.TagIds);
                await _courseTagRepository.SaveChangesAsync();
            }

            return CreatedAtAction(
                nameof(Get),
                new { id = entity.Id },
                new
                {
                    success = true,
                    message = "Course created successfully.",
                    data = new CourseResponse(entity)
                });
        }

        // ─────────────────────────────────────────────────────────
        // PUT {id} — update
        // ─────────────────────────────────────────────────────────
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CourseRequest model)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return NotFound();

            var (success, errors) = await model.ValidateCourseAsync(_baseRepository, id);
            if (!success)
                return BadRequest(new { status = 400, message = "Validation failed", errors = FlattenErrors(errors) });

            model.ToEntity(entity);
            await _baseRepository.SaveChangesAsync();

            // Sync tags if provided
            if (model.TagIds != null)
            {
                await entity.SyncTagsAsync(_courseTagRepository, _tagRepository, model.TagIds);
                await _courseTagRepository.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = "Course updated successfully.",
                data = new CourseResponse(entity)
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /delete — bulk soft-delete (matches FE courseServices.deleteCourses)
        // ─────────────────────────────────────────────────────────
        [HttpPut("delete")]
        public async Task<IActionResult> Delete([FromBody] List<long> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one course id." });

            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ToListAsync();

            if (!entities.Any()) return NotFound();

            foreach (var entity in entities)
            {
                entity.IsDeleted = true;
                entity.IsActive = false;
                entity.UpdatedAt = DateTime.UtcNow;
            }

            await _baseRepository.SaveChangesAsync();
            return Ok(new { success = true, deleted = entities.Count });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [HttpPut("disable")]
        public async Task<IActionResult> Disable([FromBody] List<long> ids)
        {
            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
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
        [HttpPut("enable")]
        public async Task<IActionResult> Enable([FromBody] List<long> ids)
        {
            var entities = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
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
        // GET /suggest?keyword=... — quick search for autocomplete
        // ─────────────────────────────────────────────────────────
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Ok(Array.Empty<object>());

            var lower = keyword.ToLower();
            var courses = await _baseRepository.Query()
                .Where(x => !x.IsDeleted && x.IsActive && x.Title.ToLower().Contains(lower))
                .OrderBy(x => x.Title)
                .Select(x => new { x.Id, x.Title, x.ImageUrl })
                .Take(10)
                .ToListAsync();

            return Ok(courses);
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
