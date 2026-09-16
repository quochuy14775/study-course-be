using System.Text.RegularExpressions;

namespace StudyCourseAPI.Extensions;

/// <summary>Chuẩn hóa màu thương hiệu / nhóm do admin nhập cho Language & Framework.</summary>
public static partial class BrandExtensions
{
    private static readonly string[] Categories =
        { "frontend", "backend", "mobile", "fullstack", "styling", "testing", "data", "devops" };

    [GeneratedRegex("^#[0-9a-fA-F]{6}$")]
    private static partial Regex HexColor();

    /// <summary>"#61dafb" → "#61DAFB"; chuỗi không phải #RRGGBB → null (FE tự gợi ý).</summary>
    public static string? NormalizeBrandColor(this string? value)
    {
        var v = value?.Trim();
        return v is not null && HexColor().IsMatch(v) ? v.ToUpperInvariant() : null;
    }

    /// <summary>Chỉ nhận nhóm trong danh sách cho phép, viết thường; khác → null.</summary>
    public static string? NormalizeCategory(this string? value)
    {
        var v = value?.Trim().ToLowerInvariant();
        return v is not null && Categories.Contains(v) ? v : null;
    }
}
