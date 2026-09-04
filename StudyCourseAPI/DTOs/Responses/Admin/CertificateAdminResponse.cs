using StudyCourseAPI.Models;

namespace StudyCourseAPI.DTOs.Responses.Admin;

/// <summary>Admin listing — adds learner identity fields the learner-facing DTO doesn't need.</summary>
public class CertificateAdminResponse
{
    public long Id { get; set; }
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? UserEmail { get; set; }
    public string CertificateCode { get; set; } = null!;
    public DateTime IssuedAt { get; set; }
    public double ScorePercentage { get; set; }

    public CertificateAdminResponse(Certificate c)
    {
        Id = c.Id;
        CourseId = c.CourseId;
        CourseTitle = c.Course?.Title ?? string.Empty;
        UserId = c.UserId;
        UserName = c.User?.FullName ?? c.User?.UserName ?? c.User?.Email ?? "Học viên";
        UserEmail = c.User?.Email;
        CertificateCode = c.CertificateCode;
        IssuedAt = c.IssuedAt;
        ScorePercentage = c.ScorePercentage;
    }
}
