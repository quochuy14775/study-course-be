using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>
/// Phía admin soạn đề. Upsert thay thế toàn bộ bộ câu hỏi/đáp án thay vì diff từng câu —
/// nội dung quiz nhỏ và do admin tự soạn nên replace đơn giản hơn nhiều.
/// </summary>
public interface IQuizManagementService
{
    Task<QuizResponse?> GetLessonQuizAsync(long lessonId);

    Task<ServiceResult<QuizResponse>> UpsertLessonQuizAsync(long lessonId, QuizRequest model);

    Task<bool> DeleteLessonQuizAsync(long lessonId);

    Task<QuizResponse?> GetCourseTestAsync(long courseId);

    Task<ServiceResult<QuizResponse>> UpsertCourseTestAsync(long courseId, QuizRequest model);

    Task<bool> DeleteCourseTestAsync(long courseId);
}
