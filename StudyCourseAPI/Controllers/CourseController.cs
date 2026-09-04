using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers
{
    [Route("api/[controller]")]
    [Authorize]
    public class CoursesController : BaseController<Course>
    {
        private readonly IRepository<CourseTag> _courseTagRepository;
        private readonly IRepository<Tag> _tagRepository;
        private readonly IRepository<CourseLanguage> _courseLanguageRepository;
        private readonly IRepository<CourseFramework> _courseFrameworkRepository;
        private readonly IRepository<Language> _languageRepository;
        private readonly IRepository<Framework> _frameworkRepository;
        private readonly INotificationService _notifier;

        public CoursesController(
            IRepository<Course> baseRepository,
            ICurrentUser currentUser,
            IRepository<CourseTag> courseTagRepository,
            IRepository<Tag> tagRepository,
            IRepository<CourseLanguage> courseLanguageRepository,
            IRepository<CourseFramework> courseFrameworkRepository,
            IRepository<Language> languageRepository,
            IRepository<Framework> frameworkRepository,
            INotificationService notifier)
            : base(baseRepository, currentUser)
        {
            _courseTagRepository = courseTagRepository;
            _tagRepository = tagRepository;
            _courseLanguageRepository = courseLanguageRepository;
            _courseFrameworkRepository = courseFrameworkRepository;
            _languageRepository = languageRepository;
            _frameworkRepository = frameworkRepository;
            _notifier = notifier;
        }

        // ─────────────────────────────────────────────────────────
        // GET — list with OData (public — no auth required)
        // AsSplitQuery avoids cartesian explosion with multiple Includes.
        // AppendQueryOptions already applies AsNoTracking.
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get(ODataQueryOptions<Course> queryOptions)
        {
            var queryable = _baseRepository.Query()
                .Where(x => !x.IsDeleted && x.IsActive)
                .Include(c => c.CourseLanguages).ThenInclude(cl => cl.Language)
                .Include(c => c.CourseFrameworks).ThenInclude(cf => cf.Framework)
                .AsSplitQuery();

            var (count, vm) = await queryable.AppendQueryOptionsAsync(queryOptions);

            return Ok(new ODataResponse<CourseResponse>
            {
                Count = count,
                Value = vm.Select(x => new CourseResponse(x))
            });
        }

        // ─────────────────────────────────────────────────────────
        // GET single (public)
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var course = await _baseRepository.Query()
                .AsNoTracking()
                .Include(c => c.CourseTags)
                .Include(c => c.CourseLanguages).ThenInclude(cl => cl.Language)
                .Include(c => c.CourseFrameworks).ThenInclude(cf => cf.Framework)
                .AsSplitQuery()
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

            if (course == null) return NotFound();
            return Ok(new CourseDetailResponse(course));
        }

        // ─────────────────────────────────────────────────────────
        // POST
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] CourseRequest model)
        {
            var (success, errors) = await model.ValidateCourseAsync(_baseRepository);
            if (!success)
                return this.ValidationFailed(errors);

            var entity = model.GetCourse();
            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            // Sync tags after course id is available
            if (model.TagIds != null && model.TagIds.Any())
            {
                await entity.SyncTagsAsync(_courseTagRepository, _tagRepository, model.TagIds);
            }

            if (model.LanguageIds != null)
            {
                await entity.SyncLanguagesAsync(_courseLanguageRepository, _languageRepository, model.LanguageIds);
            }

            if (model.FrameworkIds != null)
            {
                await entity.SyncFrameworksAsync(_courseFrameworkRepository, _frameworkRepository, model.FrameworkIds);
            }

            // Broadcast new-course notification to all users
            await _notifier.NotifyAllAsync(
                $"🎓 Khoá học mới: {entity.Title}",
                NotificationType.Info,
                $"/courses/{entity.Id}/learn",
                actorId: _currentUser.GetCurrentUserId());

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
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] CourseRequest model)
        {
            var entity = await _baseRepository.Query()
                .Include(c => c.CourseTags)
                .Include(c => c.CourseLanguages)
                .Include(c => c.CourseFrameworks)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entity == null) return NotFound();

            var (success, errors) = await model.ValidateCourseAsync(_baseRepository, id);
            if (!success)
                return this.ValidationFailed(errors);

            model.ToEntity(entity);
            await _baseRepository.SaveChangesAsync();

            // Sync tags
            if (model.TagIds != null)
            {
                await entity.SyncTagsAsync(_courseTagRepository, _tagRepository, model.TagIds);
            }

            if (model.LanguageIds != null)
            {
                await entity.SyncLanguagesAsync(_courseLanguageRepository, _languageRepository, model.LanguageIds);
            }

            if (model.FrameworkIds != null)
            {
                await entity.SyncFrameworksAsync(_courseFrameworkRepository, _frameworkRepository, model.FrameworkIds);
            }

            return Ok(new
            {
                success = true,
                message = "Course updated successfully.",
                data = new CourseResponse(entity)
            });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /delete — bulk soft-delete via ExecuteUpdateAsync (no entity load)
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("delete")]
        public async Task<IActionResult> Delete([FromBody] List<long> ids)
        {
            if (ids == null || ids.Count == 0)
                return BadRequest(new { status = 400, message = "Provide at least one course id." });

            var now = DateTime.UtcNow;
            var affected = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsDeleted, true)
                    .SetProperty(c => c.IsActive, false)
                    .SetProperty(c => c.UpdatedAt, now));

            return affected == 0
                ? NotFound()
                : Ok(new { success = true, deleted = affected });
        }

        // ─────────────────────────────────────────────────────────
        // PUT /disable — bulk
        // ─────────────────────────────────────────────────────────
        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("disable")]
        public async Task<IActionResult> Disable([FromBody] List<long> ids)
        {
            var now = DateTime.UtcNow;
            var affected = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
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
        public async Task<IActionResult> Enable([FromBody] List<long> ids)
        {
            var now = DateTime.UtcNow;
            var affected = await _baseRepository.Query()
                .Where(x => ids.Contains(x.Id) && !x.IsDeleted)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(c => c.IsActive, true)
                    .SetProperty(c => c.UpdatedAt, now));

            return affected == 0 ? NotFound() : NoContent();
        }

        // ─────────────────────────────────────────────────────────
        // GET /suggest?keyword=... — quick search for autocomplete (public)
        // ─────────────────────────────────────────────────────────
        [AllowAnonymous]
        [HttpGet("suggest")]
        public async Task<IActionResult> Suggest([FromQuery] string? keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return Ok(Array.Empty<object>());

            // EF.Functions.ILike is PostgreSQL native case-insensitive match — uses index, no full-table .ToLower()
            var pattern = $"%{keyword}%";
            var courses = await _baseRepository.Query()
                .AsNoTracking()
                .Where(x => !x.IsDeleted && x.IsActive && EF.Functions.ILike(x.Title, pattern))
                .OrderBy(x => x.Title)
                .Select(x => new { x.Id, x.Title, x.ImageUrl })
                .Take(10)
                .ToListAsync();

            return Ok(courses);
        }
    }
}
