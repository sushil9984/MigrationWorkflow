using MongoDB.Driver;
using MigrationWorkflow.Domain.Entities;

namespace MigrationWorkflow.Infrastructure.Data;

/// <summary>
/// MongoDB context for accessing Party collection
/// </summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;
    
    public MongoDbContext(string connectionString, string databaseName)
    {
        var client = new MongoClient(connectionString);
        _database = client.GetDatabase(databaseName);
    }
    
    public IMongoCollection<Party> Parties => 
        _database.GetCollection<Party>("Party");
}
