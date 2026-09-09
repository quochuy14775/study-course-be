using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class FrameworkService : IFrameworkService
{
    private readonly IRepository<Framework> _frameworkRepository;
    private readonly IRepository<LanguageFramework> _languageFrameworkRepository;
    private readonly IRepository<Language> _languageRepository;

    public FrameworkService(
        IRepository<Framework> frameworkRepository,
        IRepository<LanguageFramework> languageFrameworkRepository,
        IRepository<Language> languageRepository)
    {
        _frameworkRepository = frameworkRepository;
        _languageFrameworkRepository = languageFrameworkRepository;
        _languageRepository = languageRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<List<FrameworkResponse>> GetListAsync()
    {
        var frameworks = await _frameworkRepository.Query()
            .AsNoTracking()
            .Where(f => !f.IsDeleted && f.IsActive)
            .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
            .AsSplitQuery()
            .OrderBy(f => f.Name)
            .ToListAsync();

        return frameworks.Select(f => new FrameworkResponse(f)).ToList();
    }

    public async Task<FrameworkResponse?> GetByIdAsync(long id)
    {
        var entity = await _frameworkRepository.Query()
            .AsNoTracking()
            .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
            .AsSplitQuery()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        return entity == null ? null : new FrameworkResponse(entity);
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<FrameworkResponse>> CreateAsync(FrameworkRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return ServiceResult<FrameworkResponse>.Invalid("Name is required.");

        if (string.IsNullOrWhiteSpace(model.Slug))
            return ServiceResult<FrameworkResponse>.Invalid("Slug is required.");

        if (await _frameworkRepository.Query().AnyAsync(f => f.Slug == model.Slug && !f.IsDeleted))
            return ServiceResult<FrameworkResponse>.Invalid("Slug already exists.");

        var entity = new Framework
        {
            Name = model.Name.Trim(),
            Slug = model.Slug.Trim().ToLower(),
            IconUrl = model.IconUrl?.Trim(),
            IsActive = model.IsActive
        };
        _frameworkRepository.Add(entity);
        await _frameworkRepository.SaveChangesAsync();

        await SyncLanguagesAsync(entity.Id, model.LanguageIds);

        var created = await _frameworkRepository.Query()
            .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
            .FirstAsync(f => f.Id == entity.Id);

        return ServiceResult<FrameworkResponse>.Ok(new FrameworkResponse(created));
    }

    public async Task<ServiceResult<FrameworkResponse>> UpdateAsync(long id, FrameworkRequest model)
    {
        var entity = await _frameworkRepository.Query()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (entity == null)
            return ServiceResult<FrameworkResponse>.NotFound();

        if (await _frameworkRepository.Query().AnyAsync(f => f.Slug == model.Slug && f.Id != id && !f.IsDeleted))
            return ServiceResult<FrameworkResponse>.Invalid("Slug already exists.");

        entity.Name = model.Name.Trim();
        entity.Slug = model.Slug.Trim().ToLower();
        entity.IconUrl = model.IconUrl?.Trim();
        entity.IsActive = model.IsActive;
        await _frameworkRepository.SaveChangesAsync();

        if (model.LanguageIds != null)
            await SyncLanguagesAsync(entity.Id, model.LanguageIds);

        var updated = await _frameworkRepository.Query()
            .Include(f => f.LanguageFrameworks).ThenInclude(lf => lf.Language)
            .FirstAsync(f => f.Id == entity.Id);

        return ServiceResult<FrameworkResponse>.Ok(new FrameworkResponse(updated));
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        var entity = await _frameworkRepository.Query()
            .FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        await _frameworkRepository.SaveChangesAsync();

        return true;
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private Task SyncLanguagesAsync(long frameworkId, List<long>? languageIds)
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

        return _languageFrameworkRepository.SyncLinksAsync(
            currentTask,
            validIdsTask,
            lf => lf.LanguageId,
            langId => new LanguageFramework { LanguageId = langId, FrameworkId = frameworkId });
    }
}
