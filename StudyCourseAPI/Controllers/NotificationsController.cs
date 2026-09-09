using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly IUserNotificationService _notificationService;

    public NotificationsController(IUserNotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? top = 20, [FromQuery] bool? unreadOnly = false)
    {
        return Ok(await _notificationService.GetAllAsync(top, unreadOnly));
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _notificationService.GetUnreadCountAsync();
        return Ok(new { count });
    }

    // ── Mark-read: atomic update, no entity load ─────────────────────────────
    [HttpPut("{id:long}/read")]
    public async Task<IActionResult> MarkRead(long id)
    {
        // null = không tồn tại; true/false = vừa đánh dấu / đã đọc từ trước
        var marked = await _notificationService.MarkReadAsync(id);

        return marked == null ? NotFound() : NoContent();
    }

    // ── Mark-all-read: bulk atomic ───────────────────────────────────────────
    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var affected = await _notificationService.MarkAllReadAsync();
        return Ok(new { marked = affected });
    }

    // ── Delete: ExecuteDelete = single SQL, no load ──────────────────────────
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var deleted = await _notificationService.DeleteAsync(id);

        return deleted ? NoContent() : NotFound();
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAll()
    {
        var affected = await _notificationService.ClearAllAsync();
        return Ok(new { deleted = affected });
    }
}
