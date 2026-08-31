namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Current status of a workflow execution
/// </summary>
public class WorkflowStatus
{
    public string WorkflowId { get; set; } = string.Empty;
    public WorkflowStage CurrentStage { get; set; }
    public double ProgressPercentage { get; set; }
    public string StatusMessage { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
    public bool IsCompleted { get; set; }
    public bool HasErrors { get; set; }
}

/// <summary>
/// Stages of the workflow
/// </summary>
public enum WorkflowStage
{
    NotStarted = 0,
    Migration = 1,
    Reconciliation = 2,
    Analysis = 3,
    Completed = 4,
    Failed = 5
}
