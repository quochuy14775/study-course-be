using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Extensions
{
    public static class NoteExtensions
    {
        public static (bool Success, Dictionary<string, List<string>>? Errors)
            ValidateNote(this NoteRequest model)
        {
            var errors = new Dictionary<string, List<string>>();

            void Add(string key, string msg)
            {
                if (!errors.ContainsKey(key)) errors[key] = new List<string>();
                errors[key].Add(msg);
            }

            if (string.IsNullOrWhiteSpace(model.Content))
                Add("content", "Content is required.");
            else if (model.Content.Length > 5000)
                Add("content", "Content cannot exceed 5000 characters.");

            if (model.VideoTimestamp < 0)
                Add("videoTimestamp", "Video timestamp cannot be negative.");

            return (errors.Count == 0, errors.Count == 0 ? null : errors);
        }

        public static LessonNote GetNote(this NoteRequest model, long lessonId, long userId)
        {
            return new LessonNote
            {
                LessonId = lessonId,
                UserId = userId,
                Content = model.Content,
                VideoTimestamp = model.VideoTimestamp,
            };
        }

        public static void MapTo(this NoteRequest model, LessonNote entity)
        {
            entity.Content = model.Content;
            entity.VideoTimestamp = model.VideoTimestamp;
            entity.UpdatedAt = DateTime.UtcNow;
        }
    }
}
