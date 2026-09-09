using Microsoft.AspNetCore.Identity;
using StudyCourseAPI.DTOs.Requests.User;
using StudyCourseAPI.DTOs.Responses.User;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class UserProfileService : IUserProfileService
{
    private readonly ICurrentUser _currentUser;
    private readonly UserManager<ApplicationUser> _userManager;

    public UserProfileService(ICurrentUser currentUser, UserManager<ApplicationUser> userManager)
    {
        _currentUser = currentUser;
        _userManager = userManager;
    }

    public async Task<UserProfileResponse?> GetMeAsync()
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user);
        return UserProfileResponse.UserProfile(user, roles);
    }

    public async Task<ServiceResult<UserProfileResponse>?> UpdateProfileAsync(UpdateProfileRequest request)
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return null;

        request.MapTo(user);

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return ServiceResult<UserProfileResponse>.Invalid(Describe(result));

        var roles = await _userManager.GetRolesAsync(user);
        return ServiceResult<UserProfileResponse>.Ok(UserProfileResponse.UserProfile(user, roles));
    }

    public async Task<ServiceResult<bool>?> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var user = _currentUser.GetCurrentUser();
        if (user is null) return null;

        var result = await _userManager.ChangePasswordAsync(
            user, request.CurrentPassword, request.NewPassword);

        return result.Succeeded
            ? ServiceResult<bool>.Ok(true)
            : ServiceResult<bool>.Invalid(Describe(result));
    }

    private static List<string> Describe(IdentityResult result)
        => result.Errors.Select(e => e.Description).ToList();
}
