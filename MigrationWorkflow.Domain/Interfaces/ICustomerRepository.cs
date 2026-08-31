using MigrationWorkflow.Domain.Entities;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Repository for accessing Customer table in PostgreSQL
/// </summary>
public interface ICustomerRepository
{
    Task<Customer?> GetBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default);
    Task<int> GetCountAsync(CancellationToken cancellationToken = default);
    Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);
    Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
