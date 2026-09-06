using Microsoft.AspNetCore.Mvc;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WorkflowController(
    IWorkflowOrchestrator orchestrator,
    IAnalysisNarrativeService analysisNarrativeService,
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

    /// <summary>
    /// Generate and inspect the LLM prompt and raw response used for analysis enrichment.
    /// If no request body is supplied, a sample reconciliation payload is used.
    /// </summary>
    [HttpPost("analysis-debug")]
    public async Task<ActionResult<AnalysisDebugResponse>> DebugAnalysisNarrative(
        [FromBody] AnalysisRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var analysisRequest = request ?? CreateSampleAnalysisRequest();
            var currentAnalysis = CreateBaselineAnalysis(analysisRequest.ReconciliationReport);

            var debugResponse = await analysisNarrativeService.GenerateDebugResponseAsync(
                analysisRequest,
                currentAnalysis,
                cancellationToken);

            if (!debugResponse.IsEnabled)
            {
                return BadRequest(debugResponse);
            }

            if (!string.IsNullOrWhiteSpace(debugResponse.ErrorMessage))
            {
                return StatusCode(502, debugResponse);
            }

            return Ok(debugResponse);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating analysis debug response");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    private static AnalysisRequest CreateSampleAnalysisRequest()
    {
        return new AnalysisRequest
        {
            GenerateDetailedAnalysis = true,
            ReconciliationReport = new ReconciliationReport
            {
                ReportId = Guid.NewGuid().ToString(),
                MigrationId = Guid.NewGuid().ToString(),
                GeneratedAt = DateTime.UtcNow,
                SourceRecordCount = 1000,
                TargetRecordCount = 992,
                MismatchCount = 8,
                DataAccuracyPercentage = 99.19,
                IsReconciled = false,
                Summary = "Reconciliation completed. Source: 1000, Target: 992, Mismatches: 8, Accuracy: 99.19%",
                Discrepancies = new List<ReconciliationDiscrepancy>
                {
                    new()
                    {
                        RecordId = "PARTY-1001",
                        FieldName = "Email",
                        SourceValue = "john.doe@example.com",
                        TargetValue = "johnny.doe@example.com",
                        DiscrepancyType = "Mismatch"
                    },
                    new()
                    {
                        RecordId = "PARTY-1034",
                        FieldName = "Phone",
                        SourceValue = "+1234567890",
                        TargetValue = string.Empty,
                        DiscrepancyType = "Mismatch"
                    },
                    new()
                    {
                        RecordId = "PARTY-1090",
                        FieldName = string.Empty,
                        SourceValue = "Not Found",
                        TargetValue = "CUST-1090",
                        DiscrepancyType = "Missing"
                    }
                }
            }
        };
    }

    private static AnalysisResult CreateBaselineAnalysis(ReconciliationReport report)
    {
        var result = new AnalysisResult
        {
            ReportId = report.ReportId,
            AnalyzedAt = DateTime.UtcNow,
            QualityScore = report.DataAccuracyPercentage,
            OverallStatus = report.DataAccuracyPercentage switch
            {
                >= 99.0 => "Success",
                >= 95.0 => "Warning",
                _ => "Failed"
            },
            DiscrepancyTypeBreakdown = report.Discrepancies
                .GroupBy(d => d.DiscrepancyType)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        result.KeyFindings.Add($"Total records processed: {report.TargetRecordCount}");
        result.KeyFindings.Add($"Data accuracy: {report.DataAccuracyPercentage:F2}%");
        result.KeyFindings.Add($"Migration is {(report.IsReconciled ? "fully" : "not fully")} reconciled");

        if (report.SourceRecordCount != report.TargetRecordCount)
        {
            result.KeyFindings.Add($"Count mismatch detected: {Math.Abs(report.SourceRecordCount - report.TargetRecordCount)} records difference");
            result.Recommendations.Add("Investigate missing or extra records in target database");
        }

        if (report.DataAccuracyPercentage < 100)
        {
            result.Recommendations.Add("Review and correct data discrepancies before proceeding");
        }

        var missingRecords = report.Discrepancies.Count(d => d.DiscrepancyType == "Missing");
        if (missingRecords > 0)
        {
            result.Recommendations.Add($"Re-run migration for {missingRecords} missing records");
        }

        var mismatchRecords = report.Discrepancies.Count(d => d.DiscrepancyType == "Mismatch");
        if (mismatchRecords > 0)
        {
            result.Recommendations.Add($"Validate transformation logic for {mismatchRecords} mismatched records");
        }

        if (report.DataAccuracyPercentage < 90)
        {
            result.CriticalIssues.Add($"CRITICAL: Low data accuracy ({report.DataAccuracyPercentage:F2}%)");
        }

        var countDiff = Math.Abs(report.SourceRecordCount - report.TargetRecordCount);
        if (countDiff > report.SourceRecordCount * 0.05)
        {
            result.CriticalIssues.Add($"CRITICAL: Significant record count mismatch ({countDiff} records)");
        }

        if (missingRecords > 10)
        {
            result.CriticalIssues.Add($"CRITICAL: High number of missing records ({missingRecords})");
        }

        return result;
    }
}
