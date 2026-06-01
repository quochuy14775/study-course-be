using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class SkillResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public SkillResponse(Skill skill)
        {
            Id = skill.Id;
            Name = skill.Name;
            Description = skill.Description;
            IconUrl = skill.IconUrl;
            IsActive = skill.IsActive;
            CreatedAt = skill.CreatedAt;
            UpdatedAt = skill.UpdatedAt;
        }
    }

    public class CourseSkillResponse
    {
        public long SkillId { get; set; }
        public string SkillName { get; set; } = null!;
        public double ContributionPercentage { get; set; }

        public CourseSkillResponse(CourseSkill cs)
        {
            SkillId = cs.SkillId;
            SkillName = cs.Skill?.Name ?? "Unknown";
            ContributionPercentage = cs.ContributionPercentage;
        }
    }
}

