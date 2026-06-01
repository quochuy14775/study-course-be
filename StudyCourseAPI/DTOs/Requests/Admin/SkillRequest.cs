namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class SkillRequest
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class CourseSkillRequest
    {
        public long SkillId { get; set; }
        public double ContributionPercentage { get; set; }
    }
}

