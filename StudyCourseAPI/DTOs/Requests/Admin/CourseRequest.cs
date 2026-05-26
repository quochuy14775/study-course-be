using StudyCourseAPI.Enums;

namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class CourseRequest
    {
        public string? Title { get; set; }

        public string? Subtitle { get; set; }

        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }

        public CourseLevel Level { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>Optional list of tag ids to associate with this course.</summary>
        public List<long>? TagIds { get; set; }
    }
}
