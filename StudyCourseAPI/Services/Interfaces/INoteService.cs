using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>
/// Ghi chú của người học trên từng lesson. Mọi method đều tự giới hạn theo user đang
/// đăng nhập — không nhận userId từ ngoài vào để tránh sửa nhầm note của người khác.
/// </summary>
public interface INoteService
{
    Task<List<NoteResponse>> GetByLessonAsync(long lessonId);

    Task<ServiceResult<NoteResponse>> CreateAsync(long lessonId, NoteRequest model);

    Task<ServiceResult<NoteResponse>> UpdateAsync(long lessonId, long noteId, NoteRequest model);

    /// <summary>Soft-delete note của chính user. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long lessonId, long noteId);
}
