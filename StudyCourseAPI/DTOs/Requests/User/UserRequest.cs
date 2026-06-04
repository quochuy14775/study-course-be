using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests.User;

public class UpdateProfileRequest
{
    [Required]
    [MaxLength(100)]
    public string FullName { get; set; } = null!;

    [MaxLength(500)]
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordRequest
{
    [Required]
    public string CurrentPassword { get; set; } = null!;

    [Required]
    [MinLength(6)]
    public string NewPassword { get; set; } = null!;
}
