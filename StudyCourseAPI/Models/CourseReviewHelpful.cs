namespace StudyCourseAPI.Models;

public class CourseReviewHelpful
{
    public long ReviewId { get; set; }
    public CourseReview Review { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
