using System.Diagnostics;
using Microsoft.Extensions.Logging;
using MigrationWorkflow.Domain.Entities;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Application.Agents;

/// <summary>
/// Agent responsible for migrating data from MongoDB Party collection to PostgreSQL Customer table
/// </summary>
public class MigrationAgent : AgentBase<MigrationRequest, MigrationResult>, IMigrationAgent
{
    private readonly IPartyRepository _partyRepository;
    private readonly ICustomerRepository _customerRepository;
    
    public override string AgentName => "MigrationAgent";
    
    public MigrationAgent(
        IPartyRepository partyRepository,
        ICustomerRepository customerRepository,
        ILogger<MigrationAgent> logger) : base(logger)
    {
        _partyRepository = partyRepository;
        _customerRepository = customerRepository;
    }
    
    public async Task<int> GetTotalRecordsToMigrateAsync(CancellationToken cancellationToken = default)
    {
        return (int)await _partyRepository.GetCountAsync(cancellationToken: cancellationToken);
    }
    
    public override async Task<MigrationResult> ExecuteAsync(
        MigrationRequest input,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult
        {
            CompletedAt = DateTime.UtcNow
        };
        
        try
        {
            LogInfo($"Starting migration from {input.CollectionName} to {input.TargetTableName}");
            
            var totalCount = await _partyRepository.GetCountAsync(
                input.FromDate,
                input.ToDate,
                cancellationToken);
            
            LogInfo($"Found {totalCount} records to migrate");
            result.TotalRecordsProcessed = (int)totalCount;
            
            // Process in batches
            var batchSize = input.BatchSize;
            var skip = 0;
            
            while (skip < totalCount)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var parties = await _partyRepository.GetPartiesAsync(
                    skip,
                    batchSize,
                    input.FromDate,
                    input.ToDate,
                    cancellationToken);
                
                foreach (var party in parties)
                {
                    try
                    {
                        var customer = MapPartyToCustomer(party);
                        
                        // Check if customer already exists
                        var existing = await _customerRepository.GetBySourceIdAsync(
                            party.Id ?? "",
                            cancellationToken);
                        
                        if (existing != null)
                        {
                            // Update existing
                            UpdateCustomerFromParty(existing, party);
                            await _customerRepository.UpdateAsync(existing, cancellationToken);
                        }
                        else
                        {
                            // Insert new
                            await _customerRepository.AddAsync(customer, cancellationToken);
                        }
                        
                        result.SuccessfulMigrations++;
                    }
                    catch (Exception ex)
                    {
                        result.FailedMigrations++;
                        result.Errors.Add($"Failed to migrate Party {party.Id}: {ex.Message}");
                        LogError($"Failed to migrate Party {party.Id}", ex);
                    }
                }
                
                await _customerRepository.SaveChangesAsync(cancellationToken);
                skip += batchSize;
                
                LogInfo($"Migrated batch {skip}/{totalCount} records");
            }
            
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = result.FailedMigrations == 0;
            
            LogInfo($"Migration completed. Success: {result.SuccessfulMigrations}, Failed: {result.FailedMigrations}, Duration: {result.Duration}");
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            LogError("Migration process failed", ex);
        }
        
        return result;
    }
    
    private Customer MapPartyToCustomer(Party party)
    {
        return new Customer
        {
            CustomerId = party.PartyId,
            Name = party.Name,
            CustomerType = party.Type,
            Email = party.Email,
            Phone = party.Phone,
            Street = party.Address?.Street,
            City = party.Address?.City,
            State = party.Address?.State,
            ZipCode = party.Address?.ZipCode,
            Country = party.Address?.Country,
            Status = party.Status,
            CreatedDate = party.CreatedDate,
            ModifiedDate = party.ModifiedDate,
            SourceId = party.Id,
            MigratedAt = DateTime.UtcNow
        };
    }
    
    private void UpdateCustomerFromParty(Customer customer, Party party)
    {
        customer.CustomerId = party.PartyId;
        customer.Name = party.Name;
        customer.CustomerType = party.Type;
        customer.Email = party.Email;
        customer.Phone = party.Phone;
        customer.Street = party.Address?.Street;
        customer.City = party.Address?.City;
        customer.State = party.Address?.State;
        customer.ZipCode = party.Address?.ZipCode;
        customer.Country = party.Address?.Country;
        customer.Status = party.Status;
        customer.ModifiedDate = party.ModifiedDate;
        customer.MigratedAt = DateTime.UtcNow;
    }
}
