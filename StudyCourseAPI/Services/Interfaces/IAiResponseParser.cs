namespace StudyCourseAPI.Services;

/// <summary>
/// Lightweight heuristic parsing of free-form AI text into structured fields
/// (key points, hints, code issues...). Not real NLP — just keyword/structure
/// matching good enough to populate the response DTOs.
/// </summary>
public interface IAiResponseParser
{
    List<string> ExtractKeyPoints(string text);
    string ExtractSummary(string text);
    string ExtractMisunderstandings(string text);
    string ExtractSteps(string text);
    string ExtractHint(string text);
    List<string> ExtractConcepts(string text);
    List<string> ExtractCodeIssues(string text);
    List<string> ExtractSuggestions(string text);
    List<string> ExtractBestPractices(string text);
    string ExtractImprovedCode(string text);
}
