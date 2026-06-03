namespace StudyCourseAPI.Models;

public class LanguageFramework
{
    public long LanguageId { get; set; }
    public Language Language { get; set; } = null!;

    public long FrameworkId { get; set; }
    public Framework Framework { get; set; } = null!;
}
