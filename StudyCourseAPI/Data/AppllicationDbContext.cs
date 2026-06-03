using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using StudyCourseAPI.Enums;
using StudyCourseAPI.Models;

namespace StudyCourseAPI.Data;

public class ApplicationDbContext
    : IdentityDbContext<
        ApplicationUser,
        Role,
        long,
        IdentityUserClaim<long>,
        UserRole,
        IdentityUserLogin<long>,
        IdentityRoleClaim<long>,
        IdentityUserToken<long>>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ========================
    // DbSets
    // ========================
    public DbSet<Course> Courses => Set<Course>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<Lesson> Lessons => Set<Lesson>();
    public DbSet<UserCourse> UserCourses => Set<UserCourse>();
    public DbSet<UserLessonProgress> UserLessonProgresses => Set<UserLessonProgress>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<CourseTag> CourseTags => Set<CourseTag>();
    public DbSet<CourseBookmark> CourseBookmarks => Set<CourseBookmark>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Language> Languages => Set<Language>();
    public DbSet<Framework> Frameworks => Set<Framework>();
    public DbSet<LanguageFramework> LanguageFrameworks => Set<LanguageFramework>();
    public DbSet<CourseLanguage> CourseLanguages => Set<CourseLanguage>();
    public DbSet<CourseFramework> CourseFrameworks => Set<CourseFramework>();

    // Learning interactions
    public DbSet<LessonNote> LessonNotes => Set<LessonNote>();
    public DbSet<LessonComment> LessonComments => Set<LessonComment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<LessonQuestion> LessonQuestions => Set<LessonQuestion>();
    public DbSet<QuestionAnswer> QuestionAnswers => Set<QuestionAnswer>();
    public DbSet<AnswerLike> AnswerLikes => Set<AnswerLike>();

    // ========================
    // Model Config
    // ========================
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // ========================
        // USER - ROLE
        // ========================
        builder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });

            entity.HasOne(x => x.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(x => x.RoleId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ========================
        // USER-COURSE (enrollment)
        // ========================
        builder.Entity<UserCourse>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.CourseId });

            entity.HasOne(x => x.User)
                .WithMany(u => u.UserCourses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany(c => c.UserCourses)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.UserId);
            entity.HasIndex(x => x.CourseId);

            entity.Property(x => x.EnrolledAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(x => x.Progress).HasDefaultValue(0);
        });

        // ========================
        // COURSE
        // ========================
        builder.Entity<Course>(entity =>
        {
            entity.Property(x => x.Title).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.Subtitle).HasMaxLength(500);
            entity.Property(x => x.ImageUrl).HasMaxLength(500);
            entity.Property(x => x.Price).HasColumnType("decimal(18,2)").HasDefaultValue(0);
            entity.Property(e => e.Level).HasConversion<int>().HasDefaultValue(CourseLevel.Beginner);
            entity.Property(x => x.IsFeatured).HasDefaultValue(false);
            entity.Property(x => x.Rating).HasDefaultValue(0);
            entity.Property(x => x.TotalDurationSeconds).HasDefaultValue(0);
            entity.Property(x => x.LessonCount).HasDefaultValue(0);
            entity.Property(x => x.ChapterCount).HasDefaultValue(0);

            entity.HasIndex(x => x.IsFeatured);
            entity.HasIndex(x => new { x.IsActive, x.IsDeleted });
        });

        // ========================
        // CHAPTER
        // ========================
        builder.Entity<Chapter>(entity =>
        {
            entity.Property(x => x.Title).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(c => c.Course)
                .WithMany(cs => cs.Chapters)
                .HasForeignKey(c => c.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.CourseId, x.OrderIndex });
        });

        // ========================
        // LESSON
        // ========================
        builder.Entity<Lesson>(entity =>
        {
            entity.Property(x => x.Title).IsRequired().HasMaxLength(255);
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.VideoId).IsRequired().HasMaxLength(255);
            entity.Property(x => x.ThumbnailUrl).HasMaxLength(500);
            entity.Property(x => x.IsPreview).HasDefaultValue(false);

            entity.HasOne(l => l.Course)
                .WithMany(c => c.Lessons)
                .HasForeignKey(l => l.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            // Lesson optionally belongs to a Chapter (null when uncategorized)
            entity.HasOne(l => l.Chapter)
                .WithMany(ch => ch.Lessons)
                .HasForeignKey(l => l.ChapterId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(x => new { x.CourseId, x.OrderIndex });
            entity.HasIndex(x => new { x.ChapterId, x.OrderIndex });
        });

        // ========================
        // USER-LESSON PROGRESS
        // ========================
        builder.Entity<UserLessonProgress>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.LessonId });

            entity.HasOne(x => x.User)
                .WithMany(u => u.LessonProgresses)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Lesson)
                .WithMany(l => l.Progresses)
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.WatchedSeconds).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => new { x.UserId, x.CourseId });
            entity.HasIndex(x => new { x.UserId, x.IsCompleted });
        });

        // ========================
        // TAG (self-referencing for Language → Framework)
        // ========================
        builder.Entity<Tag>(entity =>
        {
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Icon).HasMaxLength(128);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(t => t.ParentTag)
                .WithMany()
                .HasForeignKey(t => t.ParentTagId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => new { x.Type, x.ParentTagId });
        });

        // ========================
        // COURSE-TAG (M-N)
        // ========================
        builder.Entity<CourseTag>(entity =>
        {
            entity.HasKey(x => new { x.CourseId, x.TagId });

            entity.HasOne(x => x.Course)
                .WithMany(c => c.CourseTags)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Tag)
                .WithMany(t => t.CourseTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.TagId);
        });

        // ========================
        // COURSE BOOKMARK (wishlist)
        // ========================
        builder.Entity<CourseBookmark>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.CourseId });

            entity.HasOne(x => x.User)
                .WithMany(u => u.Bookmarks)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Course)
                .WithMany(c => c.Bookmarks)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.Property(x => x.IsActive).HasDefaultValue(true);
        });

        // ========================
        // NOTIFICATION
        // ========================
        builder.Entity<Notification>(entity =>
        {
            entity.Property(x => x.Message).IsRequired().HasMaxLength(1000);
            entity.Property(x => x.LinkUrl).HasMaxLength(500);
            entity.Property(x => x.Type).HasConversion<int>().HasDefaultValue(NotificationType.Info);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.UserId, x.IsRead, x.CreatedAt });
        });

        // ========================
        // LANGUAGE
        // ========================
        builder.Entity<Language>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(64);
            entity.Property(x => x.IconUrl).HasMaxLength(500);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        // ========================
        // FRAMEWORK
        // ========================
        builder.Entity<Framework>(entity =>
        {
            entity.Property(x => x.Name).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Slug).IsRequired().HasMaxLength(64);
            entity.Property(x => x.IconUrl).HasMaxLength(500);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        // ========================
        // LANGUAGE-FRAMEWORK (M-N)
        // ========================
        builder.Entity<LanguageFramework>(entity =>
        {
            entity.HasKey(x => new { x.LanguageId, x.FrameworkId });

            entity.HasOne(x => x.Language)
                .WithMany(l => l.LanguageFrameworks)
                .HasForeignKey(x => x.LanguageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Framework)
                .WithMany(f => f.LanguageFrameworks)
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.FrameworkId);
        });

        // ========================
        // COURSE-LANGUAGE (M-N)
        // ========================
        builder.Entity<CourseLanguage>(entity =>
        {
            entity.HasKey(x => new { x.CourseId, x.LanguageId });

            entity.HasOne(x => x.Course)
                .WithMany(c => c.CourseLanguages)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Language)
                .WithMany(l => l.CourseLanguages)
                .HasForeignKey(x => x.LanguageId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.LanguageId);
        });

        // ========================
        // COURSE-FRAMEWORK (M-N)
        // ========================
        builder.Entity<CourseFramework>(entity =>
        {
            entity.HasKey(x => new { x.CourseId, x.FrameworkId });

            entity.HasOne(x => x.Course)
                .WithMany(c => c.CourseFrameworks)
                .HasForeignKey(x => x.CourseId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.Framework)
                .WithMany(f => f.CourseFrameworks)
                .HasForeignKey(x => x.FrameworkId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => x.FrameworkId);
        });

        // ========================
        // LESSON NOTE
        // ========================
        builder.Entity<LessonNote>(entity =>
        {
            entity.Property(x => x.Content).IsRequired().HasMaxLength(5000);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(x => new { x.LessonId, x.UserId });
        });

        // ========================
        // LESSON COMMENT
        // ========================
        builder.Entity<LessonComment>(entity =>
        {
            entity.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.LikeCount).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(x => x.ParentComment)
                .WithMany(c => c.Replies)
                .HasForeignKey(x => x.ParentCommentId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.LessonId);
            entity.HasIndex(x => x.ParentCommentId);
        });

        // ========================
        // COMMENT LIKE
        // ========================
        builder.Entity<CommentLike>(entity =>
        {
            entity.HasKey(x => new { x.CommentId, x.UserId });

            entity.HasOne(x => x.Comment)
                .WithMany(c => c.Likes)
                .HasForeignKey(x => x.CommentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================
        // LESSON QUESTION
        // ========================
        builder.Entity<LessonQuestion>(entity =>
        {
            entity.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.AnswerCount).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Lesson)
                .WithMany()
                .HasForeignKey(x => x.LessonId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.LessonId);
        });

        // ========================
        // QUESTION ANSWER
        // ========================
        builder.Entity<QuestionAnswer>(entity =>
        {
            entity.Property(x => x.Content).IsRequired().HasMaxLength(2000);
            entity.Property(x => x.LikeCount).HasDefaultValue(0);
            entity.Property(x => x.IsActive).HasDefaultValue(true);

            entity.HasOne(x => x.Question)
                .WithMany(q => q.Answers)
                .HasForeignKey(x => x.QuestionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(x => x.QuestionId);
        });

        // ========================
        // ANSWER LIKE
        // ========================
        builder.Entity<AnswerLike>(entity =>
        {
            entity.HasKey(x => new { x.AnswerId, x.UserId });

            entity.HasOne(x => x.Answer)
                .WithMany(a => a.Likes)
                .HasForeignKey(x => x.AnswerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ========================
        // SOFT DELETE FILTERS
        // ========================
        builder.Entity<Course>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<CourseTag>().HasQueryFilter(x => !x.Course.IsDeleted);
        builder.Entity<Chapter>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Lesson>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<UserCourse>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<UserLessonProgress>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Tag>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<CourseBookmark>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Notification>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Language>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<Framework>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<LessonNote>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<LessonComment>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<LessonQuestion>().HasQueryFilter(x => !x.IsDeleted);
        builder.Entity<QuestionAnswer>().HasQueryFilter(x => !x.IsDeleted);
    }

    // ========================
    // AUDIT TRACKING
    // ========================
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.CreatedAt = DateTime.UtcNow;
                if (entry.State == EntityState.Modified)
                    auditable.UpdatedAt = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
