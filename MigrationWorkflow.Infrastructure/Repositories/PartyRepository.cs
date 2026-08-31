using MongoDB.Driver;
using MigrationWorkflow.Domain.Entities;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Infrastructure.Data;

namespace MigrationWorkflow.Infrastructure.Repositories;

public class PartyRepository : IPartyRepository
{
    private readonly MongoDbContext _context;
    
    public PartyRepository(MongoDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<Party>> GetPartiesAsync(
        int skip,
        int limit,
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Party>.Filter;
        var filter = FilterDefinition<Party>.Empty;
        
        if (fromDate.HasValue)
            filter &= filterBuilder.Gte(p => p.CreatedDate, fromDate.Value);
        if (toDate.HasValue)
            filter &= filterBuilder.Lte(p => p.CreatedDate, toDate.Value);
        
        return await _context.Parties
            .Find(filter)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }
    
    public async Task<long> GetCountAsync(
        DateTime? fromDate = null,
        DateTime? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var filterBuilder = Builders<Party>.Filter;
        var filter = FilterDefinition<Party>.Empty;
        
        if (fromDate.HasValue)
            filter &= filterBuilder.Gte(p => p.CreatedDate, fromDate.Value);
        if (toDate.HasValue)
            filter &= filterBuilder.Lte(p => p.CreatedDate, toDate.Value);
        
        return await _context.Parties.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }
    
    public async Task<Party?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        return await _context.Parties
            .Find(p => p.Id == id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
