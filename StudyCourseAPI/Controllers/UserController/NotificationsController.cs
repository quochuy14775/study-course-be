using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.UserController;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : BaseController<Notification>
{
    public NotificationsController(IRepository<Notification> repo, ICurrentUser currentUser)
        : base(repo, currentUser) { }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? top = 20, [FromQuery] bool? unreadOnly = false)
    {
        var userId = _currentUser.GetCurrentUserId();
        var q = _baseRepository.Query().AsNoTracking().Where(n => n.UserId == userId);

        if (unreadOnly == true) q = q.Where(n => !n.IsRead);

        // Projection inside query → only fetch needed columns, no tracking overhead
        var list = await q.OrderByDescending(n => n.CreatedAt)
            .Take(top ?? 20)
            .Select(n => new NotificationResponse
            {
                Id        = n.Id,
                Message   = n.Message,
                Type      = n.Type,
                LinkUrl   = n.LinkUrl,
                IsRead    = n.IsRead,
                CreatedAt = n.CreatedAt,
            }).ToListAsync();

        return Ok(list);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = _currentUser.GetCurrentUserId();
        var count = await _baseRepository.Query()
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead);
        return Ok(new { count });
    }

    // ── Mark-read: atomic update, no entity load ─────────────────────────────
    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        var userId = _currentUser.GetCurrentUserId();
        var now = DateTime.UtcNow;
        var affected = await _baseRepository.Query()
            .Where(n => n.Id == id && n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        return affected == 0
            ? (await _baseRepository.Query().AsNoTracking().AnyAsync(n => n.Id == id && n.UserId == userId)
                ? NoContent()      // already read
                : NotFound())
            : NoContent();
    }

    // ── Mark-all-read: bulk atomic ───────────────────────────────────────────
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = _currentUser.GetCurrentUserId();
        var now = DateTime.UtcNow;
        var affected = await _baseRepository.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s
                .SetProperty(n => n.IsRead, true)
                .SetProperty(n => n.ReadAt, now));

        return Ok(new { marked = affected });
    }

    // ── Delete: ExecuteDelete = single SQL, no load ──────────────────────────
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var userId = _currentUser.GetCurrentUserId();
        var affected = await _baseRepository.Query()
            .Where(x => x.Id == id && x.UserId == userId)
            .ExecuteDeleteAsync();

        return affected == 0 ? NotFound() : NoContent();
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAll()
    {
        var userId = _currentUser.GetCurrentUserId();
        var affected = await _baseRepository.Query()
            .Where(n => n.UserId == userId)
            .ExecuteDeleteAsync();

        return Ok(new { deleted = affected });
    }
}
