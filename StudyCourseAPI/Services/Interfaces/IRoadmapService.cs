using Microsoft.AspNetCore.OData.Query;
using StudyCourseAPI.DTOs.Requests.Admin;
using StudyCourseAPI.DTOs.Responses.Admin;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Services;

/// <summary>Lộ trình học — nhóm nhiều course lại theo thứ tự.</summary>
public interface IRoadmapService
{
    Task<(int Count, List<RoadmapResponse> Items)> GetListAsync(ODataQueryOptions<Roadmap> queryOptions);

    Task<RoadmapResponse?> GetByIdAsync(long id);

    Task<ServiceResult<RoadmapResponse>> CreateAsync(RoadmapRequest model);

    Task<ServiceResult<RoadmapResponse>> UpdateAsync(long id, RoadmapRequest model);

    /// <summary>Soft-delete hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SoftDeleteAsync(List<long> ids);

    /// <summary>Bật/tắt hàng loạt. Trả về số bản ghi bị ảnh hưởng.</summary>
    Task<int> SetActiveAsync(List<long> ids, bool isActive);
}
