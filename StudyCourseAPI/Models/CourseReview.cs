namespace StudyCourseAPI.Models;

public class CourseReview : BaseEntity<long>, IAuditable
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public int Rating { get; set; } // 1-5
    public string Content { get; set; } = null!;
    public int HelpfulCount { get; set; }

    public ICollection<CourseReviewHelpful> Helpfuls { get; set; } = new List<CourseReviewHelpful>();
    public ICollection<CourseReviewReply> Replies { get; set; } = new List<CourseReviewReply>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
