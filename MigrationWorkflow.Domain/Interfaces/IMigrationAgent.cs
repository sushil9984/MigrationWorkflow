using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Agent responsible for migrating data from MongoDB to PostgreSQL
/// </summary>
public interface IMigrationAgent : IAgent<MigrationRequest, MigrationResult>
{
    Task<int> GetTotalRecordsToMigrateAsync(CancellationToken cancellationToken = default);
}
