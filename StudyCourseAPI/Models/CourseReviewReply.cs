namespace StudyCourseAPI.Models;

public class CourseReviewReply : BaseEntity<long>, IAuditable
{
    public long ReviewId { get; set; }
    public CourseReview Review { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Content { get; set; } = null!;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
