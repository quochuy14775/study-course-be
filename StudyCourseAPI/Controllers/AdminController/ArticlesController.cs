using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers.AdminController;

[Route("api/[controller]")]
[Authorize]
public class ArticlesController : BaseController<Article>
{
    private readonly INotificationService _notifier;

    public ArticlesController(
        IRepository<Article> baseRepository,
        INotificationService notifier,
        ICurrentUser currentUser)
        : base(baseRepository, currentUser)
    {
        _notifier = notifier;
    }

    // ── GET list (public) — AsNoTracking + PG ILike for indexed case-insensitive search ──
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] string? category, [FromQuery] string? search)
    {
        var query = _baseRepository.Query()
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

        return Ok(items.Select(a => new ArticleResponse(a)));
    }

    // ── GET single (public) ───────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("{id}")]
    public async Task<IActionResult> Get(long id)
    {
        var entity = await _baseRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (entity == null) return NotFound();
        return Ok(new ArticleResponse(entity));
    }

    // ── GET by slug (public) ──────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("slug/{slug}")]
    public async Task<IActionResult> GetBySlug(string slug)
    {
        var entity = await _baseRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Slug == slug && !a.IsDeleted);

        if (entity == null) return NotFound();
        return Ok(new ArticleResponse(entity));
    }

    // ── GET categories (public) ───────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("categories")]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _baseRepository.Query()
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.IsActive && a.Category != null)
            .Select(a => a.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        return Ok(categories);
    }

    // ── GET my articles (user's own) ──────────────────────────────────────────
    [HttpGet("mine")]
    public async Task<IActionResult> GetMine()
    {
        var email = _currentUser.GetCurrentUser()?.Email;
        if (email == null) return Unauthorized();

        var items = await _baseRepository.Query()
            .AsNoTracking()
            .Where(a => !a.IsDeleted && a.CreatedBy == email)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return Ok(items.Select(a => new ArticleResponse(a)));
    }

    // ── POST — any authenticated user ─────────────────────────────────────────
    [AllowAnonymous]
    [HttpPost]
    public async Task<IActionResult> Post([FromBody] ArticleRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.Title))
            return BadRequest(new { message = "Title is required." });
        if (string.IsNullOrWhiteSpace(model.Slug))
            return BadRequest(new { message = "Slug is required." });

        if (await _baseRepository.Query().AnyAsync(a => a.Slug == model.Slug && !a.IsDeleted))
            return BadRequest(new { message = "Slug already exists." });

        var currentUserEmail = _currentUser.GetCurrentUser()?.Email;

        var entity = new Article
        {
            Title           = model.Title.Trim(),
            Slug            = model.Slug.Trim().ToLower(),
            Excerpt         = model.Excerpt?.Trim(),
            Content         = model.Content?.Trim(),
            ThumbnailUrl    = model.ThumbnailUrl?.Trim(),
            Author          = model.Author?.Trim() ?? _currentUser.GetCurrentUser()?.UserName,
            Category        = model.Category?.Trim(),
            ReadTimeMinutes = model.ReadTimeMinutes,
            IsFeatured      = false, // chỉ Admin mới set featured
            IsActive        = true,
            CreatedBy       = currentUserEmail,
        };

        _baseRepository.Add(entity);
        await _baseRepository.SaveChangesAsync();

        // Broadcast to all users
        var actorId = _currentUser.GetCurrentUserId();
        await _notifier.NotifyAllAsync(
            $"📝 Bài viết mới: {entity.Title}",
            NotificationType.Info,
            $"/articles",
            actorId: actorId);

        return CreatedAtAction(nameof(Get), new { id = entity.Id }, new ArticleResponse(entity));
    }

    // ── PUT — owner hoặc Admin ────────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Put(long id, [FromBody] ArticleRequest model)
    {
        var entity = await _baseRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (entity == null) return NotFound();

        var currentUser = _currentUser.GetCurrentUser();
        var isAdmin = currentUser != null && await IsAdminAsync(currentUser);
        var isOwner = entity.CreatedBy == currentUser?.Email;

        if (!isOwner && !isAdmin)
            return Forbid();

        if (await _baseRepository.Query().AnyAsync(a => a.Slug == model.Slug && a.Id != id && !a.IsDeleted))
            return BadRequest(new { message = "Slug already exists." });

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

        await _baseRepository.SaveChangesAsync();

        // Notify article owner when their article is newly featured
        if (isAdmin && !wasFeatured && entity.IsFeatured && !string.IsNullOrEmpty(entity.CreatedBy))
        {
            var ownerId = await HttpContext.RequestServices
                .GetRequiredService<IRepository<ApplicationUser>>()
                .Query()
                .Where(u => u.Email == entity.CreatedBy)
                .Select(u => u.Id)
                .FirstOrDefaultAsync();
            if (ownerId > 0)
            {
                await _notifier.NotifyAsync(
                    ownerId,
                    $"⭐ Bài viết \"{entity.Title}\" của bạn đã được đánh dấu nổi bật!",
                    NotificationType.Success,
                    $"/articles",
                    actorId: _currentUser.GetCurrentUserId());
            }
        }

        return Ok(new ArticleResponse(entity));
    }

    // ── DELETE — owner hoặc Admin (soft) ──────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(long id)
    {
        var entity = await _baseRepository.Query()
            .FirstOrDefaultAsync(a => a.Id == id && !a.IsDeleted);

        if (entity == null) return NotFound();

        var currentUser = _currentUser.GetCurrentUser();
        var isAdmin = currentUser != null && await IsAdminAsync(currentUser);
        var isOwner = entity.CreatedBy == currentUser?.Email;

        if (!isOwner && !isAdmin)
            return Forbid();

        entity.IsDeleted  = true;
        entity.UpdatedAt  = DateTime.UtcNow;
        entity.UpdatedBy  = currentUser?.Email;
        await _baseRepository.SaveChangesAsync();

        return Ok(new { success = true });
    }

    // ── POST increment view (public) ──────────────────────────────────────────
    // Atomic increment via ExecuteUpdate — no entity load, race-safe.
    [AllowAnonymous]
    [HttpPost("{id}/view")]
    public async Task<IActionResult> IncrementView(long id)
    {
        var affected = await _baseRepository.Query()
            .Where(a => a.Id == id && !a.IsDeleted)
            .ExecuteUpdateAsync(s => s.SetProperty(a => a.ViewCount, a => a.ViewCount + 1));

        if (affected == 0) return NotFound();

        var viewCount = await _baseRepository.Query()
            .AsNoTracking()
            .Where(a => a.Id == id)
            .Select(a => a.ViewCount)
            .FirstAsync();

        return Ok(new { viewCount });
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task<bool> IsAdminAsync(ApplicationUser user)
    {
        // Lấy role từ claims trong HttpContext — tránh call DB
        return User.IsInRole(AppRoles.Admin);
    }
}
