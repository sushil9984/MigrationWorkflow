namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Result from complete workflow execution
/// </summary>
public class WorkflowResult
{
    public string WorkflowId { get; set; } = string.Empty;
    public WorkflowStage CurrentStage { get; set; }
    public WorkflowStage CompletedStage { get; set; }
    public bool Success { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? TotalDuration { get; set; }
    
    // Results from each agent
    public MigrationResult? MigrationResult { get; set; }
    public ReconciliationReport? ReconciliationReport { get; set; }
    public AnalysisResult? AnalysisResult { get; set; }
    
    // Error tracking
    public List<string> Errors { get; set; } = new();
    public string? FailedAtStage { get; set; }
}
