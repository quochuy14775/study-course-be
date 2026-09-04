using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Models;

public class Quiz : BaseEntity<long>, IAuditable
{
    public QuizType QuizType { get; set; }

    /// <summary>Required when QuizType == Lesson. Null when QuizType == CourseTest.</summary>
    public long? LessonId { get; set; }
    public Lesson? Lesson { get; set; }

    /// <summary>
    /// Always set — denormalized for fast "all quizzes of a course" queries,
    /// and the only scope FK used when QuizType == CourseTest.
    /// </summary>
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public string Title { get; set; } = null!;

    public int PassPercentage { get; set; } = 70;
    public int TimeLimitMinutes { get; set; } = 10;

    public ICollection<QuizQuestion> Questions { get; set; } = new List<QuizQuestion>();
    public ICollection<QuizAttempt> Attempts { get; set; } = new List<QuizAttempt>();

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
}
