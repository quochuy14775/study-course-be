namespace StudyCourseAPI.Services;

public class AiResponseParser : IAiResponseParser
{
    public List<string> ExtractKeyPoints(string text)
    {
        var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        return lines.Where(l => l.Contains("-") || l.Contains("•") || l.Contains("*"))
            .Select(l => l.Trim(' ', '-', '•', '*').Trim())
            .Where(l => !string.IsNullOrEmpty(l))
            .Take(5)
            .ToList();
    }

    public string ExtractSummary(string text)
    {
        var lines = text.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
        var summaryLines = lines.Skip(1).Take(3).ToList();
        return string.Join(" ", summaryLines).Trim();
    }

    public string ExtractMisunderstandings(string text)
    {
        return text.Contains("misconception") || text.Contains("misunderstanding")
            ? text.Substring(0, Math.Min(500, text.Length))
            : "None identified";
    }

    public string ExtractSteps(string text)
    {
        return text.Contains("step") ? text : "Follow the solution above carefully";
    }

    public string ExtractHint(string text)
    {
        return "Consider breaking down the problem into smaller parts";
    }

    public List<string> ExtractConcepts(string text)
    {
        return new List<string> { "Review related topics in your course materials" };
    }

    public List<string> ExtractCodeIssues(string text)
    {
        var issues = new List<string>();
        if (text.Contains("bug") || text.Contains("error")) issues.Add("Potential bugs found");
        if (text.Contains("performance")) issues.Add("Performance concerns");
        if (text.Contains("null")) issues.Add("Null reference handling");
        return issues;
    }

    public List<string> ExtractSuggestions(string text)
    {
        return new List<string>
        {
            "Add more comments to explain complex logic",
            "Consider using more descriptive variable names",
            "Add error handling"
        };
    }

    public List<string> ExtractBestPractices(string text)
    {
        return new List<string>
        {
            "Follow SOLID principles",
            "Write unit tests",
            "Use consistent naming conventions"
        };
    }

    public string ExtractImprovedCode(string text)
    {
        var codeStart = text.IndexOf("```");
        if (codeStart >= 0)
        {
            var codeEnd = text.IndexOf("```", codeStart + 3);
            if (codeEnd > codeStart)
            {
                return text.Substring(codeStart + 3, codeEnd - codeStart - 3).Trim();
            }
        }
        return "See review above for improvements";
    }
}
