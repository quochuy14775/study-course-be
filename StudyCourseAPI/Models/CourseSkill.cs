namespace StudyCourseAPI.Models;

public class CourseSkill
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    /// <summary>
    /// Percentage (0-100) that this course contributes to the skill's mastery.
    /// Example: A course could contribute 20% to "C#" skill.
    /// </summary>
    public double ContributionPercentage { get; set; }
}

