namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Result from migration agent
/// </summary>
public class MigrationResult
{
    public bool Success { get; set; }
    public int TotalRecordsProcessed { get; set; }
    public int SuccessfulMigrations { get; set; }
    public int FailedMigrations { get; set; }
    public TimeSpan Duration { get; set; }
    public List<string> Errors { get; set; } = new();
    public DateTime CompletedAt { get; set; }
    public string MigrationId { get; set; } = Guid.NewGuid().ToString();
}
