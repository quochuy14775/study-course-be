using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests;


public class LoginRequest
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}

public class RegisterRequest
{
    [Required]
    public string Username { get; set; } = null!;

    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string Password { get; set; } = null!;

    public string? FullName { get; set; }

    // Admin mới dùng
    public string? Role { get; set; }
}