namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class FrameworkRequest
    {
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; } = true;

        /// <summary>Language ids that this framework belongs to.</summary>
        public List<long>? LanguageIds { get; set; }
    }
}
