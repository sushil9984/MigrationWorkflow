namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Debug payload for inspecting the generated LLM prompt and response.
/// </summary>
public class AnalysisDebugResponse
{
    public bool IsEnabled { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string Prompt { get; set; } = string.Empty;
    public string RawResponse { get; set; } = string.Empty;
    public string CleanedJson { get; set; } = string.Empty;
    public AnalysisNarrative? Narrative { get; set; }
    public string? ErrorMessage { get; set; }
}