using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Application.Workflow;

/// <summary>
/// Orchestrates the execution of migration, reconciliation, and analysis agents
/// </summary>
public class WorkflowOrchestrator : IWorkflowOrchestrator
{
    private readonly IMigrationAgent _migrationAgent;
    private readonly IReconciliationAgent _reconciliationAgent;
    private readonly IAnalysisAgent _analysisAgent;
    private readonly ILogger<WorkflowOrchestrator> _logger;
    
    // In-memory status tracking (use a database in production)
    private static readonly ConcurrentDictionary<string, WorkflowStatus> _workflowStatuses = new();
    
    public WorkflowOrchestrator(
        IMigrationAgent migrationAgent,
        IReconciliationAgent reconciliationAgent,
        IAnalysisAgent analysisAgent,
        ILogger<WorkflowOrchestrator> logger)
    {
        _migrationAgent = migrationAgent;
        _reconciliationAgent = reconciliationAgent;
        _analysisAgent = analysisAgent;
        _logger = logger;
    }
    
    public async Task<WorkflowResult> ExecuteWorkflowAsync(
        WorkflowRequest request,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new WorkflowResult
        {
            WorkflowId = request.WorkflowId,
            StartedAt = DateTime.UtcNow,
            CurrentStage = WorkflowStage.NotStarted
        };
        
        try
        {
            _logger.LogInformation($"Starting workflow {request.WorkflowId}");
            UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Migration, 10, "Starting migration...");
            
            // Stage 1: Migration
            result.CurrentStage = WorkflowStage.Migration;
            _logger.LogInformation("Executing Migration Agent");
            result.MigrationResult = await _migrationAgent.ExecuteAsync(
                request.MigrationRequest,
                cancellationToken);
            
            if (!result.MigrationResult.Success)
            {
                result.Success = false;
                result.FailedAtStage = "Migration";
                result.Errors.AddRange(result.MigrationResult.Errors);
                result.CurrentStage = WorkflowStage.Failed;
                UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Failed, 100, "Migration failed", true);
                return result;
            }
            
            result.CompletedStage = WorkflowStage.Migration;
            UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Migration, 40, "Migration completed successfully");
            
            // Stage 2: Reconciliation (if requested)
            if (request.GenerateReconciliationReport)
            {
                result.CurrentStage = WorkflowStage.Reconciliation;
                UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Reconciliation, 50, "Starting reconciliation...");
                
                _logger.LogInformation("Executing Reconciliation Agent");
                var reconciliationRequest = new ReconciliationRequest
                {
                    MigrationId = result.MigrationResult.MigrationId,
                    MigrationResult = result.MigrationResult,
                    SourceCollectionName = request.MigrationRequest.CollectionName,
                    TargetTableName = request.MigrationRequest.TargetTableName
                };
                
                result.ReconciliationReport = await _reconciliationAgent.ExecuteAsync(
                    reconciliationRequest,
                    cancellationToken);
                
                result.CompletedStage = WorkflowStage.Reconciliation;
                UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Reconciliation, 70, "Reconciliation completed");
            }
            
            // Stage 3: Analysis (if requested)
            if (request.PerformAnalysis && result.ReconciliationReport != null)
            {
                result.CurrentStage = WorkflowStage.Analysis;
                UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Analysis, 80, "Starting analysis...");
                
                _logger.LogInformation("Executing Analysis Agent");
                var analysisRequest = new AnalysisRequest
                {
                    ReconciliationReport = result.ReconciliationReport,
                    GenerateDetailedAnalysis = true
                };
                
                result.AnalysisResult = await _analysisAgent.ExecuteAsync(
                    analysisRequest,
                    cancellationToken);
                
                result.CompletedStage = WorkflowStage.Analysis;
                UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Analysis, 95, "Analysis completed");
            }
            
            // Complete workflow
            stopwatch.Stop();
            result.CompletedAt = DateTime.UtcNow;
            result.TotalDuration = stopwatch.Elapsed;
            result.Success = true;
            result.CurrentStage = WorkflowStage.Completed;
            result.CompletedStage = WorkflowStage.Completed;
            
            UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Completed, 100, "Workflow completed successfully");
            
            _logger.LogInformation($"Workflow {request.WorkflowId} completed successfully in {result.TotalDuration}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Success = false;
            result.CompletedAt = DateTime.UtcNow;
            result.TotalDuration = stopwatch.Elapsed;
            result.CurrentStage = WorkflowStage.Failed;
            result.FailedAtStage = result.CurrentStage.ToString();
            result.Errors.Add($"Workflow failed: {ex.Message}");
            
            UpdateWorkflowStatus(request.WorkflowId, WorkflowStage.Failed, 100, $"Workflow failed: {ex.Message}", true);
            
            _logger.LogError(ex, $"Workflow {request.WorkflowId} failed");
        }
        
        return result;
    }
    
    public Task<WorkflowStatus> GetWorkflowStatusAsync(
        string workflowId,
        CancellationToken cancellationToken = default)
    {
        if (_workflowStatuses.TryGetValue(workflowId, out var status))
        {
            return Task.FromResult(status);
        }
        
        return Task.FromResult(new WorkflowStatus
        {
            WorkflowId = workflowId,
            CurrentStage = WorkflowStage.NotStarted,
            StatusMessage = "Workflow not found",
            IsCompleted = false
        });
    }
    
    private void UpdateWorkflowStatus(
        string workflowId,
        WorkflowStage stage,
        double progress,
        string message,
        bool hasErrors = false)
    {
        var status = new WorkflowStatus
        {
            WorkflowId = workflowId,
            CurrentStage = stage,
            ProgressPercentage = progress,
            StatusMessage = message,
            LastUpdated = DateTime.UtcNow,
            IsCompleted = stage == WorkflowStage.Completed || stage == WorkflowStage.Failed,
            HasErrors = hasErrors
        };
        
        _workflowStatuses.AddOrUpdate(workflowId, status, (key, oldValue) => status);
        _logger.LogInformation($"Workflow {workflowId}: {stage} - {progress}% - {message}");
    }
}
