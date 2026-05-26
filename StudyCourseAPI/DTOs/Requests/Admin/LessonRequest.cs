using System.ComponentModel.DataAnnotations;

namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class LessonRequest
    {
        [Range(0, int.MaxValue)]
        public int OrderIndex { get; set; }

        public string? Title { get; set; }

        public string? Description { get; set; }

        public string? VideoId { get; set; }

        /// <summary>Duration in seconds.</summary>
        public int? Duration { get; set; }

        public string? ThumbnailUrl { get; set; }

        /// <summary>Optional chapter assignment. Null = uncategorized.</summary>
        public long? ChapterId { get; set; }

        /// <summary>Null = keep existing value on update; defaults to false on create.</summary>
        public bool? IsPreview { get; set; }

        public bool IsActive { get; set; } = true;
    }

    /// <summary>DTO for creating a new chapter inline when creating lessons.</summary>
    public class CreateChapterDto
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; } = 0;
    }

    /// <summary>
    /// Wrapper body for POST /api/Courses/{courseId}/Lessons.
    /// One request = one chapter (existing or new) + its lessons.
    ///
    /// Rules (priority top → bottom):
    ///   1. If <see cref="NewChapter"/> is provided → create new chapter, assign all lessons to it.
    ///   2. Else if <see cref="ChapterId"/> is provided → use that existing chapter.
    ///   3. Else → return error (chapter is required).
    /// </summary>
    public class BulkCreateLessonsRequest
    {
        public long? ChapterId { get; set; }
        public CreateChapterDto? NewChapter { get; set; }

        public List<LessonRequest> Lessons { get; set; } = new();
    }

    /// <summary>Payload for PUT /reorder — list of (lessonId, orderIndex, optional chapterId).</summary>
    public class LessonReorderItem
    {
        public long Id { get; set; }
        public int OrderIndex { get; set; }
        public long? ChapterId { get; set; }
    }
}
