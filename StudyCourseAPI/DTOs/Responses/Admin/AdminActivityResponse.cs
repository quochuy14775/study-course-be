namespace StudyCourseAPI.DTOs.Responses.Admin;

/// <summary>
/// GET api/admin/activity?before=&amp;limit= — dòng hoạt động gần đây, ghép từ nhiều bảng.
/// Phân trang bằng cursor thời gian: gọi tiếp với before = NextCursor.
/// (Khi có bảng ActivityEvents thì thay phần ghép này bằng một truy vấn.)
/// </summary>
public class AdminActivityResponse
{
    public List<AdminActivityItemResponse> Items { get; set; } = new();
    /// <summary>CreatedAt của item cuối; null khi hết.</summary>
    public DateTime? NextCursor { get; set; }
}

/// <summary>Kind: enroll | cert_issued | review | question | course_created | course_updated.</summary>
public class AdminActivityItemResponse
{
    /// <summary>"{kind}:{id}" — duy nhất giữa các nguồn.</summary>
    public string Id { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string ActorName { get; set; } = null!;
    public long? CourseId { get; set; }
    public string? CourseTitle { get; set; }
    public long? LessonId { get; set; }
    /// <summary>Số kèm theo: điểm chứng chỉ, số sao review...</summary>
    public double? Value { get; set; }
    public DateTime CreatedAt { get; set; }
}
