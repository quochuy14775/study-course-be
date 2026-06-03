using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin
{
    public class FrameworkResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<LanguageSummaryResponse> Languages { get; set; } = new();

        public FrameworkResponse(Framework framework)
        {
            Id = framework.Id;
            Name = framework.Name;
            Slug = framework.Slug;
            IconUrl = framework.IconUrl;
            IsActive = framework.IsActive;
            CreatedAt = framework.CreatedAt;
            UpdatedAt = framework.UpdatedAt;

            if (framework.LanguageFrameworks != null)
                Languages = framework.LanguageFrameworks
                    .Where(lf => lf.Language != null && !lf.Language.IsDeleted)
                    .Select(lf => new LanguageSummaryResponse(lf.Language))
                    .ToList();
        }
    }

    public class FrameworkSummaryResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Slug { get; set; } = null!;
        public string? IconUrl { get; set; }

        public FrameworkSummaryResponse(Framework framework)
        {
            Id = framework.Id;
            Name = framework.Name;
            Slug = framework.Slug;
            IconUrl = framework.IconUrl;
        }
    }
}
