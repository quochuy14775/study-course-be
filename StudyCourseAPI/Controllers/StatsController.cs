using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

/// <summary>Số liệu công khai — không cần đăng nhập, dùng cho trang chủ.</summary>
[ApiController]
public class StatsController : ControllerBase
{
    private readonly IPublicStatsService _publicStatsService;

    public StatsController(IPublicStatsService publicStatsService)
    {
        _publicStatsService = publicStatsService;
    }

    /// <summary>Học viên, khóa đang mở, rating trung bình, tỉ lệ hoàn thành… Cache 5 phút.</summary>
    [AllowAnonymous]
    [HttpGet("api/stats/public")]
    public async Task<IActionResult> GetPublic()
    {
        return Ok(await _publicStatsService.GetAsync());
    }
}
