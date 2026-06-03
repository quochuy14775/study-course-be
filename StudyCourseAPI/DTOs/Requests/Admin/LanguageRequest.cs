namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class LanguageRequest
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Framework ids that this language supports.</summary>
        public List<long>? FrameworkIds { get; set; }
    }
}
