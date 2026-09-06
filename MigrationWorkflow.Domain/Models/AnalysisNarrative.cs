namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// LLM-generated narrative content that can enrich analysis results.
/// </summary>
public class AnalysisNarrative
{
    public string ExecutiveSummary { get; set; } = string.Empty;
    public string DetailedAnalysis { get; set; } = string.Empty;
    public List<string> KeyFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
}