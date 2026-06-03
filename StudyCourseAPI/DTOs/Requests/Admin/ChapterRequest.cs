namespace StudyCourseAPI.DTOs.Requests.Admin
{
    public class ChapterRequest
    {
        public string Title { get; set; } = null!;
        public string? Description { get; set; }
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
