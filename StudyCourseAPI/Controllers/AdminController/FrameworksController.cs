using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.AdminController
{
    [Route("api/[controller]")]
    [Authorize]
    public class FrameworksController : BaseController<Framework>
    {
        private readonly IRepository<LanguageFramework> _languageFrameworkRepository;
        private readonly IRepository<Language> _languageRepository;

        public FrameworksController(
            IRepository<Framework> baseRepository,
            ICurrentUser currentUser,
            IRepository<LanguageFramework> languageFrameworkRepository,
            IRepository<Language> languageRepository)
            : base(baseRepository, currentUser)
        {
            _languageFrameworkRepository = languageFrameworkRepository;
            _languageRepository = languageRepository;
        }

        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var frameworks = await _baseRepository.Query()
                .AsNoTracking()
                .Where(f => !f.IsDeleted && f.IsActive)
                .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
                .AsSplitQuery()
                .OrderBy(f => f.Name)
                .ToListAsync();

            return Ok(frameworks.Select(f => new FrameworkResponse(f)));
        }

        [AllowAnonymous]
        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var entity = await _baseRepository.Query()
                .AsNoTracking()
                .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (entity == null) return NotFound();
            return Ok(new FrameworkResponse(entity));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] FrameworkRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { message = "Name is required." });
            if (string.IsNullOrWhiteSpace(model.Slug))
                return BadRequest(new { message = "Slug is required." });

            if (await _baseRepository.Query().AnyAsync(f => f.Slug == model.Slug && !f.IsDeleted))
                return BadRequest(new { message = "Slug already exists." });

            var entity = new Framework
            {
                Name = model.Name.Trim(),
                Slug = model.Slug.Trim().ToLower(),
                IconUrl = model.IconUrl?.Trim(),
                IsActive = model.IsActive
            };
            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            await SyncLanguagesAsync(entity.Id, model.LanguageIds);

            var created = await _baseRepository.Query()
                .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
                .FirstAsync(f => f.Id == entity.Id);

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new FrameworkResponse(created));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] FrameworkRequest model)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (entity == null) return NotFound();

            if (await _baseRepository.Query().AnyAsync(f => f.Slug == model.Slug && f.Id != id && !f.IsDeleted))
                return BadRequest(new { message = "Slug already exists." });

            entity.Name = model.Name.Trim();
            entity.Slug = model.Slug.Trim().ToLower();
            entity.IconUrl = model.IconUrl?.Trim();
            entity.IsActive = model.IsActive;
            await _baseRepository.SaveChangesAsync();

            if (model.LanguageIds != null)
                await SyncLanguagesAsync(entity.Id, model.LanguageIds);

            var updated = await _baseRepository.Query()
                .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
                .FirstAsync(f => f.Id == entity.Id);

            return Ok(new FrameworkResponse(updated));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private async Task SyncLanguagesAsync(long frameworkId, List<long>? languageIds)
        {
            languageIds ??= new List<long>();

            var currentTask = _languageFrameworkRepository.Query()
                .Where(lf => lf.FrameworkId == frameworkId)
                .ToListAsync();

            var validIdsTask = languageIds.Count == 0
                ? Task.FromResult(new List<long>())
                : _languageRepository.Query()
                    .AsNoTracking()
                    .Where(l => languageIds.Contains(l.Id) && !l.IsDeleted)
                    .Select(l => l.Id)
                    .ToListAsync();

            await Task.WhenAll(currentTask, validIdsTask);

            var current = currentTask.Result;
            var targetIds = validIdsTask.Result.ToHashSet();
            var currentIds = current.Select(lf => lf.LanguageId).ToHashSet();

            foreach (var link in current)
            {
                if (!targetIds.Contains(link.LanguageId))
                    _languageFrameworkRepository.Remove(link);
            }

            foreach (var langId in targetIds)
            {
                if (!currentIds.Contains(langId))
                    _languageFrameworkRepository.Add(new LanguageFramework { LanguageId = langId, FrameworkId = frameworkId });
            }

            await _languageFrameworkRepository.SaveChangesAsync();
        }
    }
}
