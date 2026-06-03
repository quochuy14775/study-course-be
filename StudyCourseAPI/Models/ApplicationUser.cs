using Microsoft.AspNetCore.Identity;

namespace StudyCourseAPI.Models;

public class ApplicationUser : IdentityUser<long>, IAuditable
{
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }

    public ICollection<UserCourse> UserCourses { get; set; } = new List<UserCourse>();
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<UserLessonProgress> LessonProgresses { get; set; } = new List<UserLessonProgress>();
    public ICollection<CourseBookmark> Bookmarks { get; set; } = new List<CourseBookmark>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
