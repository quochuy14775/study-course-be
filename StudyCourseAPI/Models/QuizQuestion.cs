namespace StudyCourseAPI.Models;

public class QuizQuestion : BaseEntity<long>
{
    public long QuizId { get; set; }
    public Quiz Quiz { get; set; } = null!;

    public string Content { get; set; } = null!;
    public int OrderIndex { get; set; }
    public double Points { get; set; } = 1;

    /// <summary>
    /// Stored as a jsonb column — options are always read/written together with
    /// their question and never queried individually, so a child table would
    /// only add joins without buying any real query capability.
    /// </summary>
    public List<QuizOptionItem> Options { get; set; } = new();
}

/// <summary>Not an entity — plain shape serialized into QuizQuestion.Options (jsonb).</summary>
public class QuizOptionItem
{
    /// <summary>Stable within the question only (not a DB id) — what the FE submits as the selected answer.</summary>
    public int OptionId { get; set; }
    public string Content { get; set; } = null!;
    public bool IsCorrect { get; set; }
    public int OrderIndex { get; set; }
}
