using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>
/// Phía người học: lấy đề (ẩn đáp án), nộp bài, xem lịch sử làm bài.
/// Cổng mở course test được kiểm tra cả ở server, không chỉ ở UI.
/// </summary>
public interface IQuizAttemptService
{
    /// <summary>Quiz của 1 lesson, đã ẩn đáp án đúng. Null nếu chưa có quiz.</summary>
    Task<QuizForAttemptResponse?> GetLessonQuizAsync(long lessonId);

    /// <summary>Thẻ tóm tắt bài kiểm tra cuối khoá: meta + đã mở khoá chưa + lần làm gần nhất.</summary>
    Task<CourseTestResponse?> GetCourseTestAsync(long courseId);

    /// <summary>Đề bài kiểm tra cuối khoá để làm. Invalid nếu chưa mở khoá.</summary>
    Task<ServiceResult<QuizForAttemptResponse>> GetCourseTestToTakeAsync(long courseId);

    /// <summary>Nộp bài và chấm điểm. Pass course test sẽ cấp certificate ngay tại đây.</summary>
    Task<ServiceResult<QuizAttemptResultResponse>> SubmitAsync(long quizId, SubmitQuizAttemptRequest model);

    /// <summary>Lịch sử làm bài của chính user cho 1 quiz.</summary>
    Task<List<QuizAttemptResultResponse>> GetOwnAttemptsAsync(long quizId);
}
