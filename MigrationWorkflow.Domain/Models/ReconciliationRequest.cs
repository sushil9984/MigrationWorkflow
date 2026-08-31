namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Request for reconciliation agent
/// </summary>
public class ReconciliationRequest
{
    public string MigrationId { get; set; } = string.Empty;
    public MigrationResult MigrationResult { get; set; } = new();
    public string SourceCollectionName { get; set; } = string.Empty;
    public string TargetTableName { get; set; } = string.Empty;
}
