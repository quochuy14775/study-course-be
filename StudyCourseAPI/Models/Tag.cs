using StudyCourseAPI.Enums;

namespace StudyCourseAPI.Models;

/// <summary>
/// Tag for organizing courses + powering the Roadmap UI (Language → Framework → Topics).
/// Single table avoids 3 separate Language/Framework/Topic entities while keeping flexibility.
/// </summary>
public class Tag : BaseEntity<long>, IAuditable
{
    /// <summary>URL-safe identifier, e.g. "typescript", "react".</summary>
    public string Slug { get; set; } = null!;

    public string Name { get; set; } = null!;
    public TagType Type { get; set; }

    /// <summary>Optional icon (lucide name, ti-* class, or emoji).</summary>
    public string? Icon { get; set; }

    /// <summary>
    /// For Framework tags, points to its parent Language.
    /// e.g. Framework "react" has ParentTagId = id of Language "typescript".
    /// </summary>
    public long? ParentTagId { get; set; }
    public Tag? ParentTag { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; } = true;

    // M2M
    public ICollection<CourseTag> CourseTags { get; set; } = new List<CourseTag>();
}
