namespace StudyCourseAPI.Models;

/// <summary>
/// User-saved courses for "Đã lưu" / Wishlist menu in the header dropdown.
/// Composite key: (UserId, CourseId).
/// </summary>
public class CourseBookmark : IAuditable
{
    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
