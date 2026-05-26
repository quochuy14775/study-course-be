namespace StudyCourseAPI.Models;

/// <summary>
/// A chapter (section) groups related lessons inside a course.
/// Course 1—N Chapter 1—N Lesson.
/// Front-end CurriculumBuilder uses this to render collapsible sections.
/// </summary>
public class Chapter : BaseEntity<long>, IAuditable
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Ordering of this chapter inside its course (0-based or 1-based).</summary>
    public int OrderIndex { get; set; }

    // FK
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // Audit / soft delete
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    // Children
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
