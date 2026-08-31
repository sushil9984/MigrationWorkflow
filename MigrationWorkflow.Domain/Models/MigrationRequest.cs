namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Request for migration agent
/// </summary>
public class MigrationRequest
{
    public string CollectionName { get; set; } = "Party";
    public string TargetTableName { get; set; } = "Customer";
    public int BatchSize { get; set; } = 100;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}
