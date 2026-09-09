using StudyCourseAPI.DTOs.Requests;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>Bình luận của học viên dưới mỗi lesson, kèm reply và like.</summary>
public interface ICommentService
{
    /// <summary>Comment gốc của lesson (kèm reply), đã đánh dấu cái nào user hiện tại đã like.</summary>
    Task<List<CommentResponse>> GetByLessonAsync(long lessonId);

    /// <summary>Tạo comment hoặc reply. Reply sẽ bắn thông báo cho chủ comment cha.</summary>
    Task<ServiceResult<CommentResponse>> CreateAsync(long lessonId, CommentRequest model);

    /// <summary>Soft-delete comment của chính user. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long lessonId, long commentId);

    /// <summary>Bật/tắt like. Null nếu comment không tồn tại.</summary>
    Task<(bool Liked, int LikeCount)?> ToggleLikeAsync(long lessonId, long commentId);
}
