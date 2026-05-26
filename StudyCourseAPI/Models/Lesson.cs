namespace StudyCourseAPI.Models;

public class Lesson : BaseEntity<long>, IAuditable
{
    /// <summary>Order of this lesson within its chapter (or course if uncategorized).</summary>
    public int OrderIndex { get; set; }

    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>YouTube/Vimeo video id (not full URL).</summary>
    public string VideoId { get; set; } = null!;

    public int? Duration { get; set; } // seconds
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// True when lesson is free preview (visible without enrollment).
    /// Matches Udemy/Coursera "preview" pattern.
    /// </summary>
    public bool IsPreview { get; set; } = false;

    // FK — Course (denormalized for fast queries even when ChapterId is null)
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    // FK — Chapter (nullable: lesson can be uncategorized)
    public long? ChapterId { get; set; }
    public Chapter? Chapter { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Soft delete
    public bool IsDeleted { get; set; }
    public bool IsActive { get; set; }

    // Children — per-user progress
    public ICollection<UserLessonProgress> Progresses { get; set; } = new List<UserLessonProgress>();
}
