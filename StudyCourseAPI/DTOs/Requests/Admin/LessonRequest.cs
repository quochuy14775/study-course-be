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

        public bool IsPreview { get; set; } = false;

        public bool IsActive { get; set; } = true;
    }

    /// <summary>Payload for PUT /reorder — list of (lessonId, orderIndex, optional chapterId).</summary>
    public class LessonReorderItem
    {
        public long Id { get; set; }
        public int OrderIndex { get; set; }
        public long? ChapterId { get; set; }
    }
}
