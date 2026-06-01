namespace StudyCourseAPI.Models;

public class LessonQuestion : BaseEntity<long>, IAuditable
{
    public long LessonId { get; set; }
    public Lesson Lesson { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public string Content { get; set; } = null!;
    public bool IsResolved { get; set; }
    public int AnswerCount { get; set; }

    public ICollection<QuestionAnswer> Answers { get; set; } = new List<QuestionAnswer>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
