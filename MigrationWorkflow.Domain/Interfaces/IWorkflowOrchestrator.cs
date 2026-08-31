using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Orchestrates the execution of multiple agents in a workflow
/// </summary>
public interface IWorkflowOrchestrator
{
    /// <summary>
    /// Executes the complete migration workflow
    /// </summary>
    Task<WorkflowResult> ExecuteWorkflowAsync(WorkflowRequest request, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets the current status of a workflow execution
    /// </summary>
    Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);
}
