using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class ArticleService : IArticleService
{
    private readonly IRepository<Article> _articleRepository;
    private readonly IRepository<ApplicationUser> _userRepository;
    private readonly INotificationService _notifier;
    private readonly ICurrentUser _currentUser;

    public ArticleService(
        IRepository<Article> articleRepository,
        IRepository<ApplicationUser> userRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
    {
        _articleRepository = articleRepository;
        _userRepository = userRepository;
        _notifier = notifier;
        _currentUser = currentUser;
    }

    // ─────────────────────────────────────────────────────────
    // Queries
    // ─────────────────────────────────────────────────────────

    public async Task<List<ArticleResponse>> SearchAsync(string? category, string? search)
    {
        var query = _articleRepository.Query()
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive);

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(a => a.Category == category);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search}%";
            query = query.Where(a => EF.Functions.ILike(a.Title, pattern)
                                  || (a.Excerpt != null && EF.Functions.ILike(a.Excerpt, pattern)));
        }

        var items = await query
            .OrderByDescending(a => a.IsFeatured)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(a => new ArticleResponse(a)).ToList();
    }

    public async Task<ArticleResponse?> GetByIdAsync(long id)
    {
        var entity = await _articleRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        return entity == null ? null : new ArticleResponse(entity);
    }

    public async Task<ArticleResponse?> GetBySlugAsync(string slug)
    {
        var entity = await _articleRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);

        return entity == null ? null : new ArticleResponse(entity);
    }

    public Task<List<string>> GetCategoriesAsync()
        => _articleRepository.Query()
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive && a.Category != null)
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

    public async Task<List<ArticleResponse>?> GetMineAsync()
    {
        var email = _currentUser.GetCurrentUser()?.Email;
        if (email == null) return null;

        var items = await _articleRepository.Query()
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.CreatedBy == email)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return items.Select(a => new ArticleResponse(a)).ToList();
    }

    // ─────────────────────────────────────────────────────────
    // Commands
    // ─────────────────────────────────────────────────────────

    public async Task<ServiceResult<ArticleResponse>> CreateAsync(ArticleRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return ServiceResult<ArticleResponse>.Invalid("Title is required.");

        if (string.IsNullOrWhiteSpace(model.Slug))
            return ServiceResult<ArticleResponse>.Invalid("Slug is required.");

        if (await _articleRepository.Query().AnyAsync(a => a.Slug == model.Slug && !a.IsDeleted))
            return ServiceResult<ArticleResponse>.Invalid("Slug already exists.");

        var user = _currentUser.GetCurrentUser();

        var entity = new Article
        {
            Title           = model.Title.Trim(),
            Slug            = model.Slug.Trim().ToLower(),
            Excerpt         = model.Excerpt?.Trim(),
            Content         = model.Content?.Trim(),
            ThumbnailUrl    = model.ThumbnailUrl?.Trim(),
            Author          = model.Author?.Trim() ?? user?.UserName,
            Category        = model.Category?.Trim(),
            ReadTimeMinutes = model.ReadTimeMinutes,
            IsFeatured      = false, // chỉ Admin mới set featured
            IsActive        = true,
            CreatedBy       = user?.Email,
        };

        _articleRepository.Add(entity);
        await _articleRepository.SaveChangesAsync();

        await _notifier.NotifyAllAsync(
            $"📝 Bài viết mới: {entity.Title}",
            NotificationType.Info,
            $"/articles",
            actorId: _currentUser.GetCurrentUserId());

        return ServiceResult<ArticleResponse>.Ok(new ArticleResponse(entity));
    }

    public async Task<ServiceResult<ArticleResponse>> UpdateAsync(long id, ArticleRequest model, bool isAdmin)
    {
        var entity = await _articleRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (entity == null)
            return ServiceResult<ArticleResponse>.NotFound();

        var currentUser = _currentUser.GetCurrentUser();
        var isOwner = entity.CreatedBy == currentUser?.Email;

        if (!isOwner && !isAdmin)
            return ServiceResult<ArticleResponse>.Forbidden();

        if (await _articleRepository.Query().AnyAsync(a => a.Slug == model.Slug && a.Id != id && !a.IsDeleted))
            return ServiceResult<ArticleResponse>.Invalid("Slug already exists.");

        entity.Title           = model.Title.Trim();
        entity.Slug            = model.Slug.Trim().ToLower();
        entity.Excerpt         = model.Excerpt?.Trim();
        entity.Content         = model.Content?.Trim();
        entity.ThumbnailUrl    = model.ThumbnailUrl?.Trim();
        entity.Author          = model.Author?.Trim();
        entity.Category        = model.Category?.Trim();
        entity.ReadTimeMinutes = model.ReadTimeMinutes;
        entity.UpdatedBy       = currentUser?.Email;

        // chỉ Admin mới được đổi IsFeatured / IsActive
        var wasFeatured = entity.IsFeatured;
        if (isAdmin)
        {
            entity.IsFeatured = model.IsFeatured;
            entity.IsActive   = model.IsActive;
        }

        await _articleRepository.SaveChangesAsync();

        // Báo cho chủ bài viết khi bài vừa được đánh dấu nổi bật
        if (isAdmin && !wasFeatured && entity.IsFeatured && !string.IsNullOrEmpty(entity.CreatedBy))
            await NotifyFeaturedAsync(entity);

        return ServiceResult<ArticleResponse>.Ok(new ArticleResponse(entity));
    }

    public async Task<ServiceResult<bool>> SoftDeleteAsync(long id, bool isAdmin)
    {
        var entity = await _articleRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (entity == null)
            return ServiceResult<bool>.NotFound();

        var currentUser = _currentUser.GetCurrentUser();
        var isOwner = entity.CreatedBy == currentUser?.Email;

        if (!isOwner && !isAdmin)
            return ServiceResult<bool>.Forbidden();

        entity.IsDeleted = true;
        entity.UpdatedAt = DateTime.UtcNow;
        entity.UpdatedBy = currentUser?.Email;
        await _articleRepository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true);
    }

    public async Task<int?> IncrementViewAsync(long id)
    {
        // Tăng atomic bằng ExecuteUpdate — không load entity, an toàn với race condition
        var affected = await _articleRepository.Query()
            .Where(a => a.Id == id && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1));

        if (affected == 0) return null;

        return await _articleRepository.Query()
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => a.ViewCount)
            .FirstAsync();
    }

    // ─────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────

    private async Task NotifyFeaturedAsync(Article entity)
    {
        var ownerId = await _userRepository.Query()
            .Where(u => u.Email == entity.CreatedBy)
            .Select(u => u.Id)
            .FirstOrDefaultAsync();

        if (ownerId <= 0) return;

        await _notifier.NotifyAsync(
            ownerId,
            $"⭐ Bài viết \"{entity.Title}\" của bạn đã được đánh dấu nổi bật!",
            NotificationType.Success,
            $"/articles",
            actorId: _currentUser.GetCurrentUserId());
    }
}
