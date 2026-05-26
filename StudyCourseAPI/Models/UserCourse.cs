namespace StudyCourseAPI.Models;

public class UserCourse : IAuditable
{
    // Composite Key
    public long UserId { get; set; } 
    public ApplicationUser User { get; set; } = null!;

    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // Business fields
    public DateTime EnrolledAt { get; set; }
    public double Progress { get; set; } // 0 → 100

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }
}