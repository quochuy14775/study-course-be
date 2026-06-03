namespace StudyCourseAPI.Models;

public class Roadmap : BaseEntity<long>, IAuditable, IUserTracking
{
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public ICollection<RoadmapCourse> RoadmapCourses { get; set; } = new List<RoadmapCourse>();
}
