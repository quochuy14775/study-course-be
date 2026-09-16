using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class LanguageService : ILanguageService
{
    private readonly IRepository<Language> _languageRepository;
    private readonly IRepository<LanguageFramework> _languageFrameworkRepository;
    private readonly IRepository<Framework> _frameworkRepository;

    public LanguageService(
        IRepository<Language> languageRepository,
        IRepository<LanguageFramework> languageFrameworkRepository,
        IRepository<Framework> frameworkRepository)
    {
        _languageRepository = languageRepository;
        _languageFrameworkRepository = languageFrameworkRepository;
        _frameworkRepository = frameworkRepository;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<List<LanguageResponse>> GetListAsync()
    {
        var languages = await _languageRepository.Query()
            .AsNoTracking()
            .Where(l => !l.IsDeleted && l.IsActive)
            .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
            .AsSplitQuery()
            .OrderBy(l => l.Name)
            .ToListAsync();

        return languages.Select(l => new LanguageResponse(l)).ToList();
    }

    public async Task<LanguageResponse?> GetByIdAsync(long id)
    {
        var entity = await _languageRepository.Query()
            .AsNoTracking()
            .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
            .AsSplitQuery()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        return entity == null ? null : new LanguageResponse(entity);
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<LanguageResponse>> CreateAsync(LanguageRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
            return ServiceResult<LanguageResponse>.Invalid("Name is required.");

        if (string.IsNullOrWhiteSpace(model.Slug))
            return ServiceResult<LanguageResponse>.Invalid("Slug is required.");

        if (await _languageRepository.Query().AnyAsync(l => l.Slug == model.Slug && !l.IsDeleted))
            return ServiceResult<LanguageResponse>.Invalid("Slug already exists.");

        var entity = new Language
        {
            Name = model.Name.Trim(),
            Slug = model.Slug.Trim().ToLower(),
            IconUrl = model.IconUrl?.Trim(),
            BrandColor = model.BrandColor.NormalizeBrandColor(),
            IsActive = model.IsActive
        };
        _languageRepository.Add(entity);
        await _languageRepository.SaveChangesAsync();

        await SyncFrameworksAsync(entity.Id, model.FrameworkIds);

        var created = await _languageRepository.Query()
            .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
            .FirstAsync(l => l.Id == entity.Id);

        return ServiceResult<LanguageResponse>.Ok(new LanguageResponse(created));
    }

    public async Task<ServiceResult<LanguageResponse>> UpdateAsync(long id, LanguageRequest model)
    {
        var entity = await _languageRepository.Query()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (entity == null)
            return ServiceResult<LanguageResponse>.NotFound();

        if (await _languageRepository.Query().AnyAsync(l => l.Slug == model.Slug && l.Id != id && !l.IsDeleted))
            return ServiceResult<LanguageResponse>.Invalid("Slug already exists.");

        entity.Name = model.Name.Trim();
        entity.Slug = model.Slug.Trim().ToLower();
        entity.IconUrl = model.IconUrl?.Trim();
        entity.BrandColor = model.BrandColor.NormalizeBrandColor();
        entity.IsActive = model.IsActive;
        await _languageRepository.SaveChangesAsync();

        if (model.FrameworkIds != null)
            await SyncFrameworksAsync(entity.Id, model.FrameworkIds);

        var updated = await _languageRepository.Query()
            .Include(l => l.LanguageFrameworks).ThenInclude(lf => lf.Framework)
            .FirstAsync(l => l.Id == entity.Id);

        return ServiceResult<LanguageResponse>.Ok(new LanguageResponse(updated));
    }

    public async Task<bool> SoftDeleteAsync(long id)
    {
        var entity = await _languageRepository.Query()
            .FirstOrDefaultAsync(l => l.Id == id && !l.IsDeleted);

        if (entity == null) return false;

        entity.IsDeleted = true;
        await _languageRepository.SaveChangesAsync();

        return true;
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private Task SyncFrameworksAsync(long languageId, List<long>? frameworkIds)
    {
        frameworkIds ??= new List<long>();

        // Chạy song song: lấy link hiện tại + validate danh sách framework id mới
        var currentTask = _languageFrameworkRepository.Query()
            .Where(lf => lf.LanguageId == languageId)
            .ToListAsync();

        var validIdsTask = frameworkIds.Count == 0
            ? Task.FromResult(new List<long>())
            : _frameworkRepository.Query()
                .AsNoTracking()
                .Where(f => frameworkIds.Contains(f.Id) && !f.IsDeleted)
                .Select(f => f.Id)
                .ToListAsync();

        return _languageFrameworkRepository.SyncLinksAsync(
            currentTask,
            validIdsTask,
            lf => lf.FrameworkId,
            fwId => new LanguageFramework { LanguageId = languageId, FrameworkId = fwId });
    }
}
