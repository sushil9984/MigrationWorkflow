using Microsoft.Extensions.Logging;
using MigrationWorkflow.Domain.Entities;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Application.Agents;

/// <summary>
/// Agent responsible for generating reconciliation reports
/// </summary>
public class ReconciliationAgent : AgentBase<ReconciliationRequest, ReconciliationReport>, IReconciliationAgent
{
    private readonly IPartyRepository _partyRepository;
    private readonly ICustomerRepository _customerRepository;
    
    public override string AgentName => "ReconciliationAgent";
    
    public ReconciliationAgent(
        IPartyRepository partyRepository,
        ICustomerRepository customerRepository,
        ILogger<ReconciliationAgent> logger) : base(logger)
    {
        _partyRepository = partyRepository;
        _customerRepository = customerRepository;
    }
    
    public override async Task<ReconciliationReport> ExecuteAsync(
        ReconciliationRequest input,
        CancellationToken cancellationToken = default)
    {
        LogInfo("Starting reconciliation report generation");
        
        var report = new ReconciliationReport
        {
            MigrationId = input.MigrationId,
            GeneratedAt = DateTime.UtcNow
        };
        
        try
        {
            // Get counts from both sources
            report.SourceRecordCount = (int)await _partyRepository.GetCountAsync(
                cancellationToken: cancellationToken);
            
            report.TargetRecordCount = await _customerRepository.GetCountAsync(cancellationToken);
            
            LogInfo($"Source count: {report.SourceRecordCount}, Target count: {report.TargetRecordCount}");
            
            // Check for discrepancies
            var customers = await _customerRepository.GetAllAsync(cancellationToken);
            
            foreach (var customer in customers)
            {
                if (string.IsNullOrEmpty(customer.SourceId))
                    continue;
                
                var party = await _partyRepository.GetByIdAsync(customer.SourceId, cancellationToken);
                
                if (party == null)
                {
                    report.Discrepancies.Add(new ReconciliationDiscrepancy
                    {
                        RecordId = customer.SourceId,
                        DiscrepancyType = "Missing",
                        SourceValue = "Not Found",
                        TargetValue = customer.CustomerId
                    });
                    continue;
                }
                
                // Data validation
                CheckFieldMismatch(report, customer, party, "Name", party.Name, customer.Name);
                CheckFieldMismatch(report, customer, party, "Email", party.Email ?? "", customer.Email ?? "");
                CheckFieldMismatch(report, customer, party, "Phone", party.Phone ?? "", customer.Phone ?? "");
            }
            
            report.MismatchCount = report.Discrepancies.Count;
            report.DataAccuracyPercentage = report.TargetRecordCount > 0
                ? ((report.TargetRecordCount - report.MismatchCount) / (double)report.TargetRecordCount) * 100
                : 0;
            
            report.IsReconciled = report.SourceRecordCount == report.TargetRecordCount 
                                  && report.MismatchCount == 0;
            
            report.Summary = $"Reconciliation completed. " +
                           $"Source: {report.SourceRecordCount}, Target: {report.TargetRecordCount}, " +
                           $"Mismatches: {report.MismatchCount}, Accuracy: {report.DataAccuracyPercentage:F2}%";
            
            LogInfo(report.Summary);
        }
        catch (Exception ex)
        {
            report.Summary = $"Reconciliation failed: {ex.Message}";
            LogError("Reconciliation failed", ex);
        }
        
        return report;
    }
    
    private void CheckFieldMismatch(
        ReconciliationReport report,
        Customer customer,
        Party party,
        string fieldName,
        string sourceValue,
        string targetValue)
    {
        if (sourceValue != targetValue)
        {
            report.Discrepancies.Add(new ReconciliationDiscrepancy
            {
                RecordId = customer.SourceId ?? "",
                FieldName = fieldName,
                SourceValue = sourceValue,
                TargetValue = targetValue,
                DiscrepancyType = "Mismatch"
            });
        }
    }
}
