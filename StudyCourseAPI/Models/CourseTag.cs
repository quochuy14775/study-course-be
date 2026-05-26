namespace StudyCourseAPI.Models;

/// <summary>Join table: Course N—N Tag.</summary>
public class CourseTag
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long TagId { get; set; }
    public Tag Tag { get; set; } = null!;
}
