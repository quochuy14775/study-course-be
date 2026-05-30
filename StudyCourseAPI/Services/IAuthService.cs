using Microsoft.AspNetCore.Identity;
using StudyCourseAPI.DTOs.Requests;

namespace StudyCourseAPI.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterRequest request);
        Task<string?>        LoginAsync(LoginRequest request);
        Task<IdentityResult> SetupAccountAsync(SetupAccountRequest request);
        Task<IdentityResult> CreateUserAsync(CreateUserRequest request);
        Task<IdentityResult> ConfirmEmailAsync(string email, string token);
        Task                 ForgotPasswordAsync(ForgotPasswordRequest request);
        Task<IdentityResult> ResetPasswordAsync(ResetPasswordRequest request);
    }
}
