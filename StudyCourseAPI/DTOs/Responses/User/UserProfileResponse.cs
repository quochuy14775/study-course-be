namespace StudyCourseAPI.DTOs.Responses.User;

public class UserProfileResponse
{
    public string Email     { get; set; } = null!;
    public string UserName  { get; set; } = null!;
    public string? FullName  { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Role      { get; set; }
}
