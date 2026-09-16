namespace StudyCourseAPI.DTOs.Responses.Admin;

/// <summary>
/// GET api/admin/inbox — mọi thứ admin cần phản hồi, tính sẵn ở server.
/// </summary>
public class AdminInboxResponse
{
    public List<AdminActionItemResponse> ActionItems { get; set; } = new();
    public List<OpenQuestionResponse> OpenQuestions { get; set; } = new();
    public List<LowReviewResponse> LowReviews { get; set; } = new();
}

/// <summary>
/// Một việc tồn đọng. Kind cố định để FE map nút hành động:
/// unanswered-questions | low-reviews | no-lesson | no-test | no-image | hidden.
/// Tone: critical | warning | info.
/// </summary>
public class AdminActionItemResponse
{
    /// <summary>Ổn định giữa các lần gọi (vd "no-test-12") để FE nhớ "đã bỏ qua".</summary>
    public string Id { get; set; } = null!;
    public string Kind { get; set; } = null!;
    public string Tone { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string Meta { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long? LessonId { get; set; }
    /// <summary>Số lượng liên quan (số câu hỏi, số review, số học viên đang học...).</summary>
    public int Count { get; set; }
}

public class OpenQuestionResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public long LessonId { get; set; }
    public string LessonTitle { get; set; } = null!;
    public string Content { get; set; } = null!;
    public int AnswerCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LowReviewResponse
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public long CourseId { get; set; }
    public string CourseTitle { get; set; } = null!;
    public int Rating { get; set; }
    public string Content { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
