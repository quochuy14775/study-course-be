namespace StudyCourseAPI.Models;

/// <summary>Junction table: Roadmap 1—N RoadmapCourse N—1 Course.</summary>
public class RoadmapCourse
{
    public long RoadmapId { get; set; }
    public Roadmap Roadmap { get; set; } = null!;

    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    /// <summary>Display order of the course within the roadmap (0-based).</summary>
    public int OrderIndex { get; set; }
}
