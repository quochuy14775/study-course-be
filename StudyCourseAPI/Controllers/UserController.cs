using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.User;
using StudyCourseAPI.Services;

namespace StudyCourseAPI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IUserActivityService _userActivityService;
    private readonly IUserOverviewService _userOverviewService;

    public UserController(
        IUserProfileService userProfileService,
        IUserActivityService userActivityService,
        IUserOverviewService userOverviewService)
    {
        _userProfileService = userProfileService;
        _userActivityService = userActivityService;
        _userOverviewService = userOverviewService;
    }

    /// <summary>Trang cá nhân: thống kê, kỹ năng, chứng chỉ, khóa đang học dở, hoạt động gần đây, thành tích.</summary>
    [HttpGet("me/overview")]
    public async Task<IActionResult> GetMyOverview()
    {
        var overview = await _userOverviewService.GetMyOverviewAsync();

        if (overview is null) return Unauthorized();
        return Ok(overview);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var profile = await _userProfileService.GetMeAsync();

        if (profile is null) return Unauthorized();
        return Ok(profile);
    }

    /// <summary>Hoạt động theo ngày (contribution graph) + streak của user đang đăng nhập. days: 7–730, mặc định 365.</summary>
    [HttpGet("me/activity")]
    public async Task<IActionResult> GetMyActivity([FromQuery] int days = 365)
    {
        return Ok(await _userActivityService.GetMyActivityAsync(days));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // null = không xác định được user đang đăng nhập
        var result = await _userProfileService.UpdateProfileAsync(request);

        if (result is null) return Unauthorized();
        if (!result.IsSuccess) return BadRequest(result.ErrorMessages);

        return Ok(result.Data);
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var result = await _userProfileService.ChangePasswordAsync(request);

        if (result is null) return Unauthorized();
        if (!result.IsSuccess) return BadRequest(result.ErrorMessages);

        return Ok();
    }
}
