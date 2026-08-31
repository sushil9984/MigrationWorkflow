using Microsoft.AspNetCore.Mvc;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController : ControllerBase
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<WorkflowController> _logger;
    
    public WorkflowController(
        IWorkflowOrchestrator orchestrator,
        ILogger<WorkflowController> logger)
    {
        _orchestrator = orchestrator;
        _logger = logger;
    }
    
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
            _logger.LogInformation($"Starting workflow: {request.WorkflowId}");
            
            var result = await _orchestrator.ExecuteWorkflowAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting workflow");
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
            var status = await _orchestrator.GetWorkflowStatusAsync(workflowId, cancellationToken);
            return Ok(status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error getting workflow status for {workflowId}");
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
            
            _logger.LogInformation($"Starting simple workflow: {request.WorkflowId}");
            
            var result = await _orchestrator.ExecuteWorkflowAsync(request, cancellationToken);
            
            if (result.Success)
            {
                return Ok(result);
            }
            
            return BadRequest(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error starting simple workflow");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
