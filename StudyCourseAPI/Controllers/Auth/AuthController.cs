using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;
using StudyCourseAPI.Services;
using StudyCourseAPI.Services;
using StudyCourseAPI.Services.Auth;

namespace StudyCourseAPI.Controllers.Auth
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;

        public AuthController(IAuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterRequest request)
        {
            var result = await _authService.RegisterAsync(request);
            return result.Succeeded
                ? Ok("Register success. Please check your email to verify your account.")
                : BadRequest(result.Errors.Select(e => e.Description));
        }

        [HttpPost("setup-account")]
        public async Task<IActionResult> SetupAccount(SetupAccountRequest request)
        {
            var result = await _authService.SetupAccountAsync(request);
            return result.Succeeded
                ? Ok("Account setup complete. You can now login.")
                : BadRequest(result.Errors.Select(e => e.Description));
        }

        [HttpGet("verify-email")]
        public async Task<IActionResult> VerifyEmail([FromQuery] string email, [FromQuery] string token)
        {
            var result = await _authService.ConfirmEmailAsync(email, token);
            return result.Succeeded ? Ok("Email verified") : BadRequest(result.Errors.Select(e => e.Description));
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordRequest request)
        {
            await _authService.ForgotPasswordAsync(request);
            return Ok("If the email exists, a reset link has been sent.");
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequest request)
        {
            var result = await _authService.ResetPasswordAsync(request);
            return result.Succeeded ? Ok("Password reset success") : BadRequest(result.Errors.Select(e => e.Description));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var token = await _authService.LoginAsync(request);
            return token is not null ? Ok(new { token }) : Unauthorized("Invalid email or password");
        }

        [HttpPost("create-user")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateUser(CreateUserRequest request)
        {
            var result = await _authService.CreateUserAsync(request);
            return result.Succeeded
                ? Ok("User created")
                : BadRequest(result.Errors.Select(e => e.Description));
        }
    }
}