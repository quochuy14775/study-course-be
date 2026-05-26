using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;
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
            return result.Succeeded ? Ok("Register success") : BadRequest(result.Errors.Select(e => e.Description));
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest request)
        {
            var token = await _authService.LoginAsync(request);
            return token is not null ? Ok(new { token }) : Unauthorized("Invalid email or password");
        }

        [HttpPost("create-user")]
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> CreateUser(RegisterRequest request)
        {
            var result = await _authService.CreateUserAsync(request);
            return Ok("User created");
        }
    }
}