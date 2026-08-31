namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Request to start a workflow
/// </summary>
public class WorkflowRequest
{
    public string WorkflowId { get; set; } = Guid.NewGuid().ToString();
    public MigrationRequest MigrationRequest { get; set; } = new();
    public bool GenerateReconciliationReport { get; set; } = true;
    public bool PerformAnalysis { get; set; } = true;
}
