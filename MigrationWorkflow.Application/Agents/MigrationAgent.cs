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
    private const int DefaultBatchSize = 100;

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
            if (batchSize <= 0)
            {
                LogWarning($"Invalid BatchSize {batchSize}; falling back to {DefaultBatchSize}");
                batchSize = DefaultBatchSize;
            }
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

                if (parties.Count == 0)
                    break; // source shrank while migrating; nothing left to page through

                await MigrateBatchAsync(parties, result, cancellationToken);
                skip += batchSize;

                LogInfo($"Migrated batch {Math.Min(skip, totalCount)}/{totalCount} records");
            }

            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;
            result.CompletedAt = DateTime.UtcNow;
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
    
    /// <summary>
    /// Stages the whole batch and saves once. If the save fails, pending changes are discarded
    /// and each record is retried on its own so only the truly bad records are counted as failed.
    /// </summary>
    private async Task MigrateBatchAsync(
        List<Party> parties,
        MigrationResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var party in parties)
            {
                await StagePartyAsync(party, cancellationToken);
            }

            await _customerRepository.SaveChangesAsync(cancellationToken);
            result.SuccessfulMigrations += parties.Count;
            return;
        }
        catch (OperationCanceledException)
        {
            _customerRepository.ClearPendingChanges();
            throw;
        }
        catch (Exception ex)
        {
            LogWarning($"Batch save failed ({ex.Message}); retrying {parties.Count} records individually");
            _customerRepository.ClearPendingChanges();
        }

        foreach (var party in parties)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await StagePartyAsync(party, cancellationToken);
                await _customerRepository.SaveChangesAsync(cancellationToken);
                result.SuccessfulMigrations++;
            }
            catch (OperationCanceledException)
            {
                _customerRepository.ClearPendingChanges();
                throw;
            }
            catch (Exception ex)
            {
                _customerRepository.ClearPendingChanges();
                result.FailedMigrations++;
                result.Errors.Add($"Failed to migrate Party {party.Id}: {ex.Message}");
                LogError($"Failed to migrate Party {party.Id}", ex);
            }
        }
    }

    private async Task StagePartyAsync(Party party, CancellationToken cancellationToken)
    {
        var existing = await _customerRepository.GetBySourceIdAsync(party.Id ?? "", cancellationToken);

        if (existing != null)
        {
            UpdateCustomerFromParty(existing, party);
            await _customerRepository.UpdateAsync(existing, cancellationToken);
        }
        else
        {
            await _customerRepository.AddAsync(MapPartyToCustomer(party), cancellationToken);
        }
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
