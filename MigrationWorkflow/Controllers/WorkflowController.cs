using Microsoft.AspNetCore.Mvc;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController(
    IWorkflowOrchestrator orchestrator,
    ILogger<WorkflowController> logger)
    : ControllerBase
{
    /// <summary>
    /// Start a new migration workflow
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<WorkflowResult>> StartWorkflow(
        [FromBody] WorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation($"Starting workflow: {request.WorkflowId}");
            
            var result = await orchestrator.ExecuteWorkflowAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Get the status of a workflow
    /// </summary>
    [HttpGet("status/{workflowId}")]
    public async Task<ActionResult<WorkflowStatus>> GetWorkflowStatus(
        string workflowId,
        CancellationToken cancellationToken)
    {
        try
        {
            var status = await orchestrator.GetWorkflowStatusAsync(workflowId, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, $"Error getting workflow status for {workflowId}");
            return StatusCode(500, new { error = ex.Message });
        }
    }
    
    /// <summary>
    /// Start a simple workflow with default settings
    /// </summary>
    [HttpPost("start-simple")]
    public async Task<ActionResult<WorkflowResult>> StartSimpleWorkflow(
        CancellationToken cancellationToken)
    {
        try
        {
            var request = new WorkflowRequest
            {
                WorkflowId = Guid.NewGuid().ToString(),
                MigrationRequest = new MigrationRequest
                {
                    CollectionName = "Party",
                    TargetTableName = "Customer",
                    BatchSize = 100
                },
                GenerateReconciliationReport = true,
                PerformAnalysis = true
            };
            
            logger.LogInformation($"Starting simple workflow: {request.WorkflowId}");
            
            var result = await orchestrator.ExecuteWorkflowAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error starting simple workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
