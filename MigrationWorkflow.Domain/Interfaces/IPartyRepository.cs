using MigrationWorkflow.Domain.Entities;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Repository for accessing Party collection in MongoDB
/// </summary>
public interface IPartyRepository
{
    Task<List<Party>> GetPartiesAsync(int skip, int limit, DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<long> GetCountAsync(DateTime? fromDate = null, DateTime? toDate = null, CancellationToken cancellationToken = default);
    Task<Party?> GetByIdAsync(string id, CancellationToken cancellationToken = default);
}
