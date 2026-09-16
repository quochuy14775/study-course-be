using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

/// <summary>
/// Số liệu cho trang Tổng quan của admin. Toàn bộ chỉ đọc; mỗi endpoint trả trọn một khối
/// để FE gọi 3 request thay vì hàng chục request lẻ.
/// </summary>
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
public class AdminDashboardController : ControllerBase
{
    private static readonly HashSet<string> Ranges = new(StringComparer.OrdinalIgnoreCase) { "7d", "30d", "90d" };

    private readonly IAdminDashboardService _dashboardService;
    private readonly IAdminProfileService _profileService;

    public AdminDashboardController(IAdminDashboardService dashboardService, IAdminProfileService profileService)
    {
        _dashboardService = dashboardService;
        _profileService = profileService;
    }

    /// <summary>Trang cá nhân của admin đang đăng nhập: đóng góp quản trị, tình trạng hệ thống, hoạt động theo ngày.</summary>
    [HttpGet("api/admin/me/overview")]
    public async Task<IActionResult> GetMyProfile()
    {
        var profile = await _profileService.GetMyProfileAsync();

        if (profile is null) return Unauthorized();
        return Ok(profile);
    }

    /// <summary>KPI, chuỗi theo ngày (kỳ này + kỳ trước), phễu, chất lượng học tập, heatmap, top khóa/bài viết.</summary>
    [HttpGet("api/admin/dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string range = "30d")
    {
        if (!Ranges.Contains(range))
            return BadRequest(new { message = "range phải là 7d, 30d hoặc 90d." });

        return Ok(await _dashboardService.GetDashboardAsync(range.ToLowerInvariant()));
    }

    /// <summary>Việc tồn đọng: khóa thiếu nội dung, câu hỏi chưa trả lời, review thấp chưa phản hồi.</summary>
    [HttpGet("api/admin/inbox")]
    public async Task<IActionResult> GetInbox()
    {
        return Ok(await _dashboardService.GetInboxAsync());
    }

    /// <summary>Dòng hoạt động gần đây. Phân trang: truyền lại <c>before = nextCursor</c> của lần trước.</summary>
    [HttpGet("api/admin/activity")]
    public async Task<IActionResult> GetActivity([FromQuery] DateTime? before, [FromQuery] int limit = 20)
    {
        return Ok(await _dashboardService.GetActivityAsync(before, limit));
    }
}
