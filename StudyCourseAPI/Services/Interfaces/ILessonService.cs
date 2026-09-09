using Microsoft.AspNetCore.OData.Query;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services;

/// <summary>Kết quả tạo hàng loạt lesson — kèm thông tin chapter mà chúng được gắn vào.</summary>
public class BulkCreateLessonsResult
{
    public List<LessonResponse> Lessons { get; init; } = new();
    public long? ChapterId { get; init; }
    public string? ChapterTitle { get; init; }

    /// <summary>True khi request tạo luôn 1 chapter mới cho đợt lesson này.</summary>
    public bool IsNewChapter { get; init; }
}

/// <summary>Use case của Lesson. Mọi thao tác đều bị giới hạn trong 1 course cụ thể.</summary>
public interface ILessonService
{
    Task<(int Count, List<LessonResponse> Items)> GetListAsync(
        long courseId, long? chapterId, ODataQueryOptions<Lesson> queryOptions);

    Task<LessonDetailResponse?> GetByIdAsync(long courseId, long id);

    /// <summary>Tạo hàng loạt lesson, tự sinh OrderIndex an toàn và refresh stats của course.</summary>
    Task<ServiceResult<BulkCreateLessonsResult>> BulkCreateAsync(long courseId, BulkCreateLessonsRequest request);

    Task<ServiceResult<LessonResponse>> UpdateAsync(long courseId, long id, LessonRequest model);

    /// <summary>Soft-delete hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SoftDeleteAsync(long courseId, List<long> ids);

    /// <summary>Bật/tắt hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SetActiveAsync(long courseId, List<long> ids, bool isActive);

    /// <summary>Sắp xếp lại thứ tự / đổi chapter cho lesson (drag-drop ở CurriculumBuilder).</summary>
    Task<ServiceResult<int>> ReorderAsync(long courseId, List<LessonReorderItem> items);
}
