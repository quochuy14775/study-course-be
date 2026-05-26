using Microsoft.AspNetCore.Identity;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role>            _roleManager;
        private readonly IJwtService                  _jwtService;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role>            roleManager,
            IJwtService                  jwtService)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _jwtService  = jwtService;
        }

        public Task<IdentityResult> RegisterAsync(RegisterRequest request)
            => CreateUserWithRoleAsync(request, AppRoles.User);

        public Task<IdentityResult> CreateUserAsync(RegisterRequest request)
        {
            var role = request.Role == AppRoles.Admin ? AppRoles.Admin : AppRoles.User;
            return CreateUserWithRoleAsync(request, role);
        }

        public async Task<string?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || user.IsDeleted || !user.IsActive)
                return null;

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            return _jwtService.GenerateToken(user, roles);
        }

        // ── private ──────────────────────────────────────────────────────────
        private async Task<IdentityResult> CreateUserWithRoleAsync(RegisterRequest request, string role)
        {
            await EnsureRolesExistAsync();

            var user = new ApplicationUser
            {
                UserName  = request.Username,
                Email     = request.Email,
                IsActive  = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) return result;

            await _userManager.AddToRoleAsync(user, role);
            return result;
        }

        private async Task EnsureRolesExistAsync()
        {
            if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
                await _roleManager.CreateAsync(new Role(AppRoles.Admin));

            if (!await _roleManager.RoleExistsAsync(AppRoles.User))
                await _roleManager.CreateAsync(new Role(AppRoles.User));
        }
    }
}