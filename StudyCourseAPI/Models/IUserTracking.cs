namespace StudyCourseAPI.Models;

public interface IUserTracking
{
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}