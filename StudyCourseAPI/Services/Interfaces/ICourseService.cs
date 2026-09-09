using Microsoft.AspNetCore.OData.Query;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services;

/// <summary>
/// Toàn bộ use case của Course. Controller chỉ gọi các method ở đây, không tự viết query.
/// </summary>
public interface ICourseService
{
    /// <summary>Danh sách course (đã lọc IsActive) kèm tổng số bản ghi, áp dụng OData query.</summary>
    Task<(int Count, List<CourseResponse> Items)> GetListAsync(ODataQueryOptions<Course> queryOptions);

    /// <summary>Chi tiết 1 course kèm tag/language/framework. Null nếu không tồn tại.</summary>
    Task<CourseDetailResponse?> GetByIdAsync(long id);

    /// <summary>Tạo course mới, đồng bộ tag/language/framework và broadcast thông báo.</summary>
    Task<ServiceResult<CourseResponse>> CreateAsync(CourseRequest model);

    /// <summary>Cập nhật course và đồng bộ lại tag/language/framework.</summary>
    Task<ServiceResult<CourseResponse>> UpdateAsync(long id, CourseRequest model);

    /// <summary>Soft-delete hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SoftDeleteAsync(List<long> ids);

    /// <summary>Bật/tắt hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SetActiveAsync(List<long> ids, bool isActive);

    /// <summary>Gợi ý course theo từ khoá (tối đa 10 kết quả) cho ô autocomplete.</summary>
    Task<List<CourseSuggestionResponse>> SuggestAsync(string? keyword);
}
