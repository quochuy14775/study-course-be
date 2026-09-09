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

    public UserController(IUserProfileService userProfileService)
    {
        _userProfileService = userProfileService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var profile = await _userProfileService.GetMeAsync();

        if (profile is null) return Unauthorized();
        return Ok(profile);
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
