using Microsoft.EntityFrameworkCore;
using MigrationWorkflow.Domain.Entities;

namespace MigrationWorkflow.Infrastructure.Data;

/// <summary>
/// PostgreSQL context for Customer entities
/// </summary>
public class PostgresDbContext : DbContext
{
    public PostgresDbContext(DbContextOptions<PostgresDbContext> options) 
        : base(options)
    {
    }
    
    public DbSet<Customer> Customers { get; set; }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasIndex(e => e.CustomerId).IsUnique();
            entity.HasIndex(e => e.SourceId);
            entity.HasIndex(e => e.Email);
        });
    }
}
