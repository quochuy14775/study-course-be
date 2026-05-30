namespace StudyCourseAPI.Configurations;

public class EmailSettings
{
    public const string SectionName = "Email";

    public string Host { get; set; } = null!;
    public int Port { get; set; } = 587;
    public bool UseStartTls { get; set; } = true;
    public string Username { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string FromName { get; set; } = "StudyCourse";
    public string FromAddress { get; set; } = null!;
}
