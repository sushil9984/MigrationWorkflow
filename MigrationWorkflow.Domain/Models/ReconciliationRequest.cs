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

    // Same CreatedDate range the migration used, so only the migrated slice is reconciled
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
