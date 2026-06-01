namespace StudyCourseAPI.Models;

public class UserSkill
{
    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    public long SkillId { get; set; }
    public Skill Skill { get; set; } = null!;

    /// <summary>
    /// Current mastery percentage (0-100+) of this skill for the user.
    /// Calculated by summing the ContributionPercentage of all completed courses containing this skill.
    /// </summary>
    public double Proficiency { get; set; }
    
    public DateTime LastUpdatedAt { get; set; }
}

