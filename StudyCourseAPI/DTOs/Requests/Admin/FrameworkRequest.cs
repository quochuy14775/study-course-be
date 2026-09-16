namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class FrameworkRequest
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>#RRGGBB — null/rỗng = để FE tự gợi ý theo slug.</summary>
        public string? BrandColor { get; set; }

        /// <summary>frontend | backend | mobile | fullstack | styling | testing | data | devops — null = chưa phân nhóm.</summary>
        public string? Category { get; set; }

        /// <summary>Language ids that this framework belongs to.</summary>
        public List<long>? LanguageIds { get; set; }
    }
}
