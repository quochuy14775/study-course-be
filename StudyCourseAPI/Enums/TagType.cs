namespace StudyCourseAPI.Enums;

/// <summary>
/// Type of a Tag. Lets one Tag table serve roadmap filters
/// (Language=TypeScript, Framework=React, Topic=Hooks).
/// </summary>
public enum TagType
{
    Topic = 0,
    Language = 1,
    Framework = 2,
}
