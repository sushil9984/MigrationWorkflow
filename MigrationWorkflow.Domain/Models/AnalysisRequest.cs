namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Request for analysis agent
/// </summary>
public class AnalysisRequest
{
    public ReconciliationReport ReconciliationReport { get; set; } = new();
    public bool GenerateDetailedAnalysis { get; set; } = true;
}
