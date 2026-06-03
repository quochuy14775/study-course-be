namespace StudyCourseAPI.Models;

public class Framework : BaseEntity<long>, IAuditable
{
    public string Name { get; set; } = null!;
    public string Slug { get; set; } = null!;
    public string? IconUrl { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<LanguageFramework> LanguageFrameworks { get; set; } = new List<LanguageFramework>();
    public ICollection<CourseFramework> CourseFrameworks { get; set; } = new List<CourseFramework>();
}
