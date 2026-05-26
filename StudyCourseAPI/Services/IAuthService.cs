using Microsoft.AspNetCore.Identity;
using StudyCourseAPI.DTOs.Requests;

namespace StudyCourseAPI.Services
{
    public interface IAuthService
    {
        Task<IdentityResult> RegisterAsync(RegisterRequest request);
        Task<string?>        LoginAsync(LoginRequest request);
        Task<IdentityResult> CreateUserAsync(RegisterRequest request);
    }
}