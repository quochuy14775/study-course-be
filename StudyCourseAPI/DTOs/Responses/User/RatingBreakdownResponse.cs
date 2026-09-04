namespace StudyCourseAPI.DTOs.Responses;

public class RatingBreakdownResponse
{
    public double Average { get; set; }
    public int Total { get; set; }

    /// <summary>Count of reviews per star (keys 1-5).</summary>
    public Dictionary<int, int> Distribution { get; set; } = new()
    {
        [1] = 0,
        [2] = 0,
        [3] = 0,
        [4] = 0,
        [5] = 0,
    };
}
