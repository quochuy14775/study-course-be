namespace StudyCourseAPI.Services;

/// <summary>
/// Tiến độ học từng lesson của user — nguồn dữ liệu mà cổng course-test của
/// QuizController đọc để quyết định user đã đủ điều kiện làm bài chưa.
/// </summary>
public interface ILessonProgressService
{
    /// <summary>Đánh dấu lesson đã hoàn thành (idempotent). False nếu lesson không tồn tại.</summary>
    Task<bool> MarkCompleteAsync(long lessonId);

    /// <summary>Danh sách id lesson user đã hoàn thành trong 1 course.</summary>
    Task<List<long>> GetCompletedLessonIdsAsync(long courseId);
}
