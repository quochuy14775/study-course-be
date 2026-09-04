using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using StudyCourseAPI.Configurations;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Data
{
    public static class DbInitializer
    {
        public static async Task PatchEmailConfirmedAsync(UserManager<ApplicationUser> userManager)
        {
            var emails = new[] { "user@gmail.com", "admin@gmail.com" };
            foreach (var email in emails)
            {
                var user = await userManager.FindByEmailAsync(email);
                if (user != null && !user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    await userManager.UpdateAsync(user);
                }
            }
        }

        public static async Task SeedAdminAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager,
            IConfiguration config)
        {
            // 1. Ensure roles
            if (!await roleManager.RoleExistsAsync(AppRoles.Admin))
                await roleManager.CreateAsync(new Role(AppRoles.Admin));

            if (!await roleManager.RoleExistsAsync(AppRoles.User))
                await roleManager.CreateAsync(new Role(AppRoles.User));

            // 2. Get admin config
            var adminConfig = config.GetSection("AdminAccount").Get<AdminAccountConfig>();

            // 3. Check admin exists
            var existingAdmin = await userManager.FindByNameAsync(adminConfig.Username);
            if (existingAdmin != null)
            {
                // ✅ Assign role nếu chưa có
                var roles = await userManager.GetRolesAsync(existingAdmin);
                if (!roles.Contains(AppRoles.Admin))
                    await userManager.AddToRoleAsync(existingAdmin, AppRoles.Admin);
    
                return;
            }

            // 4. Create admin
            var admin = new ApplicationUser
            {
                UserName = adminConfig.Username,
                Email = adminConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(admin, adminConfig.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, AppRoles.Admin);
            }
        }

        public static async Task SeedUserAsync(
            UserManager<ApplicationUser> userManager,
            RoleManager<Role> roleManager,
            IConfiguration config)
        {
            // 1. Ensure User role exists
            if (!await roleManager.RoleExistsAsync(AppRoles.User))
                await roleManager.CreateAsync(new Role(AppRoles.User));

            // 2. Get user config
            var userConfig = config.GetSection("UserAccount").Get<AdminAccountConfig>();

            // 3. Check user exists
            var existingUser = await userManager.FindByNameAsync(userConfig.Username);
            if (existingUser != null)
            {
                // ✅ Assign role nếu chưa có
                var roles = await userManager.GetRolesAsync(existingUser);
                if (!roles.Contains(AppRoles.User))
                    await userManager.AddToRoleAsync(existingUser, AppRoles.User);
    
                return;
            }

            // 4. Create user
            var user = new ApplicationUser
            {
                UserName = userConfig.Username,
                Email = userConfig.Email,
                EmailConfirmed = true,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow
            };

            var result = await userManager.CreateAsync(user, userConfig.Password);

            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(user, AppRoles.User);
            }
        }

        /// <summary>
        /// Demo content for the Quiz feature: a lesson quiz on every lesson of every course,
        /// plus a course test per course. Idempotent — skips whatever already has a quiz,
        /// same as SeedAdminAsync/SeedUserAsync above, so re-running on every startup is safe.
        /// </summary>
        public static async Task SeedQuizzesAsync(Data.ApplicationDbContext db)
        {
            var courses = await db.Courses
                .Include(c => c.Lessons)
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Id)
                .ToListAsync();

            if (courses.Count == 0) return;

            var sampleQuestions = new List<QuizQuestion>
            {
                new()
                {
                    Content = "Trong JavaScript, từ khóa nào dùng để khai báo một biến không thể gán lại giá trị?",
                    OrderIndex = 1,
                    Points = 1,
                    Options = new List<QuizOptionItem>
                    {
                        new() { OptionId = 1, Content = "var", IsCorrect = false, OrderIndex = 1 },
                        new() { OptionId = 2, Content = "let", IsCorrect = false, OrderIndex = 2 },
                        new() { OptionId = 3, Content = "const", IsCorrect = true, OrderIndex = 3 },
                        new() { OptionId = 4, Content = "static", IsCorrect = false, OrderIndex = 4 },
                    },
                },
                new()
                {
                    Content = "Kết quả của typeof \"123\" là gì?",
                    OrderIndex = 2,
                    Points = 1,
                    Options = new List<QuizOptionItem>
                    {
                        new() { OptionId = 1, Content = "\"number\"", IsCorrect = false, OrderIndex = 1 },
                        new() { OptionId = 2, Content = "\"string\"", IsCorrect = true, OrderIndex = 2 },
                        new() { OptionId = 3, Content = "\"object\"", IsCorrect = false, OrderIndex = 3 },
                        new() { OptionId = 4, Content = "\"undefined\"", IsCorrect = false, OrderIndex = 4 },
                    },
                },
                new()
                {
                    Content = "Biểu thức nào sau đây trả về NaN?",
                    OrderIndex = 3,
                    Points = 1,
                    Options = new List<QuizOptionItem>
                    {
                        new() { OptionId = 1, Content = "1 + \"1\"", IsCorrect = false, OrderIndex = 1 },
                        new() { OptionId = 2, Content = "\"a\" * 2", IsCorrect = true, OrderIndex = 2 },
                        new() { OptionId = 3, Content = "10 / 2", IsCorrect = false, OrderIndex = 3 },
                        new() { OptionId = 4, Content = "\"5\" - 1", IsCorrect = false, OrderIndex = 4 },
                    },
                },
            };

            var existingLessonQuizLessonIds = await db.Quizzes
                .Where(q => q.QuizType == QuizType.Lesson && !q.IsDeleted)
                .Select(q => q.LessonId!.Value)
                .ToListAsync();

            var existingCourseTestCourseIds = await db.Quizzes
                .Where(q => q.QuizType == QuizType.CourseTest && !q.IsDeleted)
                .Select(q => q.CourseId)
                .ToListAsync();

            foreach (var course in courses)
            {
                var lessons = course.Lessons.Where(l => !l.IsDeleted).OrderBy(l => l.OrderIndex).ToList();

                foreach (var lesson in lessons)
                {
                    if (existingLessonQuizLessonIds.Contains(lesson.Id)) continue;

                    db.Quizzes.Add(new Quiz
                    {
                        QuizType = QuizType.Lesson,
                        LessonId = lesson.Id,
                        CourseId = course.Id,
                        Title = $"Kiểm tra: {lesson.Title}",
                        PassPercentage = 70,
                        TimeLimitMinutes = 5,
                        Questions = CloneQuestions(sampleQuestions),
                    });
                }

                if (lessons.Count == 0 || existingCourseTestCourseIds.Contains(course.Id)) continue;

                db.Quizzes.Add(new Quiz
                {
                    QuizType = QuizType.CourseTest,
                    LessonId = null,
                    CourseId = course.Id,
                    Title = $"Bài test tổng kết khóa: {course.Title}",
                    PassPercentage = 70,
                    TimeLimitMinutes = 45,
                    Questions = CloneQuestions(sampleQuestions),
                });
            }

            await db.SaveChangesAsync();
        }

        private static List<QuizQuestion> CloneQuestions(List<QuizQuestion> source) =>
            source.Select(q => new QuizQuestion
            {
                Content = q.Content,
                OrderIndex = q.OrderIndex,
                Points = q.Points,
                Options = q.Options.Select(o => new QuizOptionItem
                {
                    OptionId = o.OptionId,
                    Content = o.Content,
                    IsCorrect = o.IsCorrect,
                    OrderIndex = o.OrderIndex,
                }).ToList(),
            }).ToList();
    }
}