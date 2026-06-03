namespace StudyCourseAPI.DTOs.Requests.Admin;

public class RoadmapRequest
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Ordered list of course IDs to include in this roadmap.</summary>
    public List<long> CourseIds { get; set; } = new();
}
