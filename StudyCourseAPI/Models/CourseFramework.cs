namespace StudyCourseAPI.Models;

public class CourseFramework
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long FrameworkId { get; set; }
    public Framework Framework { get; set; } = null!;
}
