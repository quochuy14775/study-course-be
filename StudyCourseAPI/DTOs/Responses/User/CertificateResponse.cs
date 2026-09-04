using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses;

public class CertificateResponse
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public string CertificateCode { get; set; } = null!;
    public double ScorePercentage { get; set; }

    public CertificateResponse(Certificate c)
    {
        Id = c.Id;
        CourseId = c.CourseId;
        CourseTitle = c.Course?.Title ?? string.Empty;
        UserName = c.User?.FullName ?? c.User?.UserName ?? c.User?.Email ?? "Học viên";
        IssuedAt = c.IssuedAt;
        CertificateCode = c.CertificateCode;
        ScorePercentage = c.ScorePercentage;
    }
}
