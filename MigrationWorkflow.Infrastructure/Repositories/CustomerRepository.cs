using Microsoft.EntityFrameworkCore;
using MigrationWorkflow.Domain.Entities;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Infrastructure.Data;

namespace MigrationWorkflow.Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly PostgresDbContext _context;
    
    public CustomerRepository(PostgresDbContext context)
    {
        _context = context;
    }
    
    public async Task<Customer?> GetBySourceIdAsync(string sourceId, CancellationToken cancellationToken = default)
    {
        return await _context.Customers
            .FirstOrDefaultAsync(c => c.SourceId == sourceId, cancellationToken);
    }
    
    public async Task<int> GetCountAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers.CountAsync(cancellationToken);
    }
    
    public async Task<List<Customer>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Customers.ToListAsync(cancellationToken);
    }
    
    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        await _context.Customers.AddAsync(customer, cancellationToken);
    }
    
    public async Task UpdateAsync(Customer customer, CancellationToken cancellationToken = default)
    {
        _context.Customers.Update(customer);
    }
    
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }
}
