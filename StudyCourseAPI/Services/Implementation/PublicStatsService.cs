using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StudyCourseAPI.Data;
using StudyCourseAPI.DTOs.Responses;

namespace StudyCourseAPI.Services;

/// <summary>
/// 7 câu COUNT/AVG nhẹ, nhưng trang chủ là trang được mở nhiều nhất nên cache 5 phút trong bộ nhớ.
/// Số lệch vài phút không ảnh hưởng — đây là social proof, không phải báo cáo.
/// </summary>
public class PublicStatsService : IPublicStatsService
{
    private const string CacheKey = "public-stats";
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(5);

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public PublicStatsService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PublicStatsResponse> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out PublicStatsResponse? cached) && cached is not null)
            return cached;

        var learners = await _db.Users.CountAsync(u => !u.IsDeleted && u.EmailConfirmed);
        var activeCourses = await _db.Courses.CountAsync(c => c.IsActive);
        var lessons = await _db.Lessons.CountAsync(l => l.IsActive);
        var enrollments = await _db.UserCourses.CountAsync();
        var completed = await _db.UserCourses.CountAsync(uc => uc.IsCompleted);
        var certificates = await _db.Certificates.CountAsync();

        var ratedCourses = _db.Courses.Where(c => c.IsActive && c.ReviewCount > 0);
        var avgRating = await ratedCourses.AnyAsync()
            ? await ratedCourses.AverageAsync(c => c.Rating)
            : 0;

        var stats = new PublicStatsResponse
        {
            Learners = learners,
            ActiveCourses = activeCourses,
            Lessons = lessons,
            Enrollments = enrollments,
            CertificatesIssued = certificates,
            AverageRating = Math.Round(avgRating, 1),
            CompletionRate = enrollments == 0 ? 0 : Math.Round(100.0 * completed / enrollments, 1),
            ComputedAt = DateTime.UtcNow,
        };

        _cache.Set(CacheKey, stats, Ttl);
        return stats;
    }
}
