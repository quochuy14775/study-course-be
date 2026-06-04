using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests.User;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Controllers.UserController;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class UserController : ControllerBase
{
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserController(ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    {
        _currentUser = currentUser;
        _userManager = userManager;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return Unauthorized();

        var roles = await _userManager.GetRolesAsync(user);

        return Ok(new UserProfileResponse
        {
            Email     = user.Email!,
            UserName  = user.UserName!,
            FullName  = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role      = roles.FirstOrDefault(),
        });
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return Unauthorized();

        user.FullName  = request.FullName.Trim();
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new UserProfileResponse
        {
            Email     = user.Email!,
            UserName  = user.UserName!,
            FullName  = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role      = roles.FirstOrDefault(),
        });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return Unauthorized();

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return Ok("Đổi mật khẩu thành công.");
    }
}
