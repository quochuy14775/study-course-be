using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services
{
    public class AuthService : IAuthService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<Role>            _roleManager;
        private readonly IJwtService                  _jwtService;
        private readonly IEmailService                _emailService;
        private readonly ILogger<AuthService>         _logger;
        private readonly string                       _frontendUrl;

        public AuthService(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role>            roleManager,
            IJwtService                  jwtService,
            IEmailService                emailService,
            ILogger<AuthService>         logger,
            IConfiguration               configuration)
        {
            _userManager  = userManager;
            _roleManager  = roleManager;
            _jwtService   = jwtService;
            _emailService = emailService;
            _logger       = logger;
            _frontendUrl  = configuration["App:FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:3000";
        }

        public async Task<IdentityResult> RegisterAsync(RegisterRequest request)
        {
            await EnsureRolesExistAsync();

            var existing = await _userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
                return IdentityResult.Failed(new IdentityError { Description = "Email already registered." });

            var user = new ApplicationUser
            {
                UserName  = request.Email, // tạm dùng email, sẽ update sau setup
                Email     = request.Email,
                IsActive  = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user);
            if (!result.Succeeded) return result;

            await _userManager.AddToRoleAsync(user, AppRoles.User);

            try
            {
                await SendConfirmationEmailAsync(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
            }

            return result;
        }

        public async Task<IdentityResult> SetupAccountAsync(SetupAccountRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found." });

            if (!user.EmailConfirmed)
                return IdentityResult.Failed(new IdentityError { Description = "Email not confirmed yet." });

            if (await _userManager.HasPasswordAsync(user))
                return IdentityResult.Failed(new IdentityError { Description = "Account already set up." });

            // Check username uniqueness
            var userWithSameName = await _userManager.FindByNameAsync(request.Username);
            if (userWithSameName is not null && userWithSameName.Id != user.Id)
                return IdentityResult.Failed(new IdentityError { Description = "Username already taken." });

            user.UserName = request.Username;
            user.FullName = request.FullName;

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded) return updateResult;

            return await _userManager.AddPasswordAsync(user, request.Password);
        }

        public async Task<IdentityResult> CreateUserAsync(CreateUserRequest request)
        {
            var role = request.Role == AppRoles.Admin ? AppRoles.Admin : AppRoles.User;

            await EnsureRolesExistAsync();

            var user = new ApplicationUser
            {
                UserName  = request.Username,
                Email     = request.Email,
                FullName  = request.FullName,
                IsActive  = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded) return result;

            await _userManager.AddToRoleAsync(user, role);
            return result;
        }

        public async Task<string?> LoginAsync(LoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user is null || user.IsDeleted || !user.IsActive)
                return null;

            if (!user.EmailConfirmed)
                return null;

            if (!await _userManager.CheckPasswordAsync(user, request.Password))
                return null;

            var roles = await _userManager.GetRolesAsync(user);
            return _jwtService.GenerateToken(user, roles);
        }

        public async Task<IdentityResult> ConfirmEmailAsync(string email, string token)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user is null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var decoded = DecodeToken(token);
            return await _userManager.ConfirmEmailAsync(user, decoded);
        }

        public async Task ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            // Không tiết lộ email có tồn tại hay không
            if (user is null || user.IsDeleted || !user.IsActive)
                return;

            var token   = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encoded = EncodeToken(token);
            var link    = $"{_frontendUrl}/reset-password?email={Uri.EscapeDataString(user.Email!)}&token={encoded}";

            var html = EmailTemplates.ResetPassword(link, user.FullName);
            await _emailService.SendAsync(user.Email!, "Đặt lại mật khẩu - EduHub", html);
        }

        public async Task<IdentityResult> ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user is null)
                return IdentityResult.Failed(new IdentityError { Description = "User not found" });

            var decoded = DecodeToken(request.Token);
            return await _userManager.ResetPasswordAsync(user, decoded, request.NewPassword);
        }

        // ── private ──────────────────────────────────────────────────────────
        private async Task SendConfirmationEmailAsync(ApplicationUser user)
        {
            var token   = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var encoded = EncodeToken(token);
            var link    = $"{_frontendUrl}/verify-email?email={Uri.EscapeDataString(user.Email!)}&token={encoded}";

            var html = EmailTemplates.Confirmation(link, user.FullName);
            await _emailService.SendAsync(user.Email!, "Xác thực tài khoản - EduHub", html);
        }

        private static string EncodeToken(string token)
            => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

        private static string DecodeToken(string encoded)
            => Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));

        private async Task EnsureRolesExistAsync()
        {
            if (!await _roleManager.RoleExistsAsync(AppRoles.Admin))
                await _roleManager.CreateAsync(new Role(AppRoles.Admin));

            if (!await _roleManager.RoleExistsAsync(AppRoles.User))
                await _roleManager.CreateAsync(new Role(AppRoles.User));
        }
    }
}
