using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;

namespace StudyCourseAPI.Services;

/// <summary>Use case của Chapter. Mọi thao tác đều bị giới hạn trong 1 course cụ thể.</summary>
public interface IChapterService
{
    /// <summary>Chapter của course, sắp theo OrderIndex. Null nếu course không tồn tại.</summary>
    Task<List<ChapterResponse>?> GetByCourseAsync(long courseId);

    Task<ChapterResponse?> GetByIdAsync(long courseId, long id);

    Task<ServiceResult<ChapterResponse>> CreateAsync(long courseId, ChapterRequest model);

    Task<ServiceResult<ChapterResponse>> UpdateAsync(long courseId, long id, ChapterRequest model);

    /// <summary>Soft-delete 1 chapter. False nếu không tìm thấy.</summary>
    Task<bool> SoftDeleteAsync(long courseId, long id);

    /// <summary>Bật/tắt hàng loạt trong 1 course. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SetActiveAsync(long courseId, List<long> ids, bool isActive);
}
