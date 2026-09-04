namespace StudyCourseAPI.Models;

public class Certificate : BaseEntity<long>
{
    public long CourseId { get; set; }
    public Course Course { get; set; } = null!;

    public long UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;

    /// <summary>Public verification code, e.g. EDU-{courseId}-{year}-{random}.</summary>
    public string CertificateCode { get; set; } = null!;

    public DateTime IssuedAt { get; set; }

    /// <summary>Snapshot of the passing course-test score — stays correct even if later attempts change it.</summary>
    public double ScorePercentage { get; set; }
}
