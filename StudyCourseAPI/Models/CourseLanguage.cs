namespace StudyCourseAPI.Models;

public class CourseLanguage
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long LanguageId { get; set; }
    public Language Language { get; set; } = null!;
}
