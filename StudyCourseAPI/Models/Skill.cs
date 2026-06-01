namespace StudyCourseAPI.Models;

public class Skill : BaseEntity<long>, IAuditable, IUserTracking
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string? IconUrl { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Relationships
    public ICollection<CourseSkill> CourseSkills { get; set; } = new List<CourseSkill>();
    public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
}

