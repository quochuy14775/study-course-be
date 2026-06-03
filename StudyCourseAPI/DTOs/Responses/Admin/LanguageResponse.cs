using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class LanguageResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<FrameworkSummaryResponse> Frameworks { get; set; } = new();

        public LanguageResponse(Language language)
        {
            Id = language.Id;
            Name = language.Name;
            Slug = language.Slug;
            IconUrl = language.IconUrl;
            IsActive = language.IsActive;
            CreatedAt = language.CreatedAt;
            UpdatedAt = language.UpdatedAt;

            if (language.LanguageFrameworks != null)
                Frameworks = language.LanguageFrameworks
                    .Where(lf => lf.Framework != null && !lf.Framework.IsDeleted)
                    .Select(lf => new FrameworkSummaryResponse(lf.Framework))
                    .ToList();
        }
    }

    public class LanguageSummaryResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }

        public LanguageSummaryResponse(Language language)
        {
            Id = language.Id;
            Name = language.Name;
            Slug = language.Slug;
            IconUrl = language.IconUrl;
        }
    }
}
