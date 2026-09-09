using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.DTOs.Responses;
using StudyCourseAPI.Extensions;
using StudyCourseAPI.Models;
using StudyCourseAPI.Repositories;

namespace StudyCourseAPI.Services;

public class EnrollmentService : IEnrollmentService
{
    private readonly IRepository<UserCourse> _userCourseRepository;
    private readonly IRepository<Course> _courseRepository;
    private readonly ICurrentUser _currentUser;

    public EnrollmentService(
        IRepository<UserCourse> userCourseRepository,
        IRepository<Course> courseRepository,
        ICurrentUser currentUser)
    {
        _userCourseRepository = userCourseRepository;
        _courseRepository = courseRepository;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<EnrollmentResponse>> EnrollAsync(long courseId)
    {
        var course = await _courseRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == courseId);

        if (course == null)
            return ServiceResult<EnrollmentResponse>.NotFound("Khóa học không tồn tại.");

        if (!course.IsActive)
            return ServiceResult<EnrollmentResponse>.Invalid("Khóa học đang tạm ngưng, chưa thể đăng ký.");

        var enrollment = await _userCourseRepository.EnsureEnrolledAsync(courseId, _currentUser.GetCurrentUserId());
        await _userCourseRepository.SaveChangesAsync();

        return ServiceResult<EnrollmentResponse>.Ok(new EnrollmentResponse(enrollment));
    }

    public async Task<EnrollmentResponse?> GetOwnByCourseAsync(long courseId)
    {
        var userId = _currentUser.GetCurrentUserId();

        var enrollment = await _userCourseRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(uc => uc.CourseId == courseId && uc.UserId == userId);

        return enrollment == null ? null : new EnrollmentResponse(enrollment);
    }

    public async Task<List<EnrolledCourseResponse>> GetMyCoursesAsync()
    {
        var userId = _currentUser.GetCurrentUserId();

        var enrollments = await _userCourseRepository.Query()
            .AsNoTracking()
            .Include(uc => uc.Course).ThenInclude(c => c.CourseLanguages).ThenInclude(cl => cl.Language)
            .Include(uc => uc.Course).ThenInclude(c => c.CourseFrameworks).ThenInclude(cf => cf.Framework)
            .Where(uc => uc.UserId == userId)
            .OrderByDescending(uc => uc.UpdatedAt ?? uc.EnrolledAt)
            .ToListAsync();

        // Course có global filter !IsDeleted nên Include trả null cho khóa đã xóa mềm — enrollment
        // trỏ tới khóa đó vẫn còn trong bảng, bỏ qua thay vì để NullReferenceException khi map.
        return enrollments
            .Where(uc => uc.Course != null)
            .Select(uc => new EnrolledCourseResponse(uc))
            .ToList();
    }
}
