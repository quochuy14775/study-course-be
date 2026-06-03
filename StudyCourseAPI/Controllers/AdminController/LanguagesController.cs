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
    public class LanguagesController : BaseController<Language>
    {
        private readonly IRepository<LanguageFramework> _languageFrameworkRepository;
        private readonly IRepository<Framework> _frameworkRepository;

        public LanguagesController(
            IRepository<Language> baseRepository,
            ICurrentUser currentUser,
            IRepository<LanguageFramework> languageFrameworkRepository,
            IRepository<Framework> frameworkRepository)
            : base(baseRepository, currentUser)
        {
            _languageFrameworkRepository = languageFrameworkRepository;
            _frameworkRepository = frameworkRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var languages = await _baseRepository.Query()
                .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
                .Where(l => !l.IsDeleted && l.IsActive)
                .OrderBy(l => l.Name)
                .ToListAsync();

            return Ok(languages.Select(l => new LanguageResponse(l)));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(long id)
        {
            var entity = await _baseRepository.Query()
                .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (entity == null) return NotFound();
            return Ok(new LanguageResponse(entity));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Post([FromBody] LanguageRequest model)
        {
            if (string.IsNullOrWhiteSpace(model.Name))
                return BadRequest(new { message = "Name is required." });
            if (string.IsNullOrWhiteSpace(model.Slug))
                return BadRequest(new { message = "Slug is required." });

            if (await _baseRepository.Query().AnyAsync(l => l.Slug == model.Slug && !l.IsDeleted))
                return BadRequest(new { message = "Slug already exists." });

            var entity = new Language
            {
                Name = model.Name.Trim(),
                Slug = model.Slug.Trim().ToLower(),
                IconUrl = model.IconUrl?.Trim(),
                IsActive = model.IsActive
            };
            _baseRepository.Add(entity);
            await _baseRepository.SaveChangesAsync();

            await SyncFrameworksAsync(entity.Id, model.FrameworkIds);

            var created = await _baseRepository.Query()
                .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
                .FirstAsync(l => l.Id == entity.Id);

            return CreatedAtAction(nameof(Get), new { id = entity.Id }, new LanguageResponse(created));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Put(long id, [FromBody] LanguageRequest model)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (entity == null) return NotFound();

            if (await _baseRepository.Query().AnyAsync(l => l.Slug == model.Slug && l.Id != id && !l.IsDeleted))
                return BadRequest(new { message = "Slug already exists." });

            entity.Name = model.Name.Trim();
            entity.Slug = model.Slug.Trim().ToLower();
            entity.IconUrl = model.IconUrl?.Trim();
            entity.IsActive = model.IsActive;
            await _baseRepository.SaveChangesAsync();

            if (model.FrameworkIds != null)
                await SyncFrameworksAsync(entity.Id, model.FrameworkIds);

            var updated = await _baseRepository.Query()
                .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
                .FirstAsync(l => l.Id == entity.Id);

            return Ok(new LanguageResponse(updated));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(long id)
        {
            var entity = await _baseRepository.Query()
                .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

            if (entity == null) return NotFound();

            entity.IsDeleted = true;
            entity.UpdatedAt = DateTime.UtcNow;
            await _baseRepository.SaveChangesAsync();

            return Ok(new { success = true });
        }

        private async Task SyncFrameworksAsync(long languageId, List<long>? frameworkIds)
        {
            frameworkIds ??= new List<long>();

            var current = await _languageFrameworkRepository.Query()
                .Where(lf => lf.LanguageId == languageId)
                .ToListAsync();

            var validIds = frameworkIds.Any()
                ? await _frameworkRepository.Query()
                    .Where(f => frameworkIds.Contains(f.Id) && !f.IsDeleted)
                    .Select(f => f.Id)
                    .ToListAsync()
                : new List<long>();

            var currentIds = current.Select(lf => lf.FrameworkId).ToHashSet();
            var targetIds = validIds.ToHashSet();

            foreach (var link in current)
            {
                if (!targetIds.Contains(link.FrameworkId))
                    await _languageFrameworkRepository.DeleteAsync(link);
            }

            foreach (var fwId in targetIds)
            {
                if (!currentIds.Contains(fwId))
                    _languageFrameworkRepository.Add(new LanguageFramework { LanguageId = languageId, FrameworkId = fwId });
            }

            await _languageFrameworkRepository.SaveChangesAsync();
        }
    }
}
