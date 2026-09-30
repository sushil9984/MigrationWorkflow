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
    private const int PageSize = 500;

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
            // Target side: only the slice covered by the migration's date range
            var customers = (await _customerRepository.GetAllAsync(cancellationToken))
                .Where(c => (!input.FromDate.HasValue || c.CreatedDate >= input.FromDate.Value)
                            && (!input.ToDate.HasValue || c.CreatedDate <= input.ToDate.Value))
                .ToList();

            var customersBySourceId = new Dictionary<string, Customer>();
            foreach (var customer in customers)
            {
                if (!string.IsNullOrEmpty(customer.SourceId))
                    customersBySourceId.TryAdd(customer.SourceId, customer);
            }

            report.SourceRecordCount = (int)await _partyRepository.GetCountAsync(
                input.FromDate,
                input.ToDate,
                cancellationToken);
            report.TargetRecordCount = customers.Count;

            LogInfo($"Source count: {report.SourceRecordCount}, Target count: {report.TargetRecordCount}");

            // Source -> target: every source party must exist in the target with matching values
            var seenSourceIds = new HashSet<string>();
            var skip = 0;
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var parties = await _partyRepository.GetPartiesAsync(
                    skip,
                    PageSize,
                    input.FromDate,
                    input.ToDate,
                    cancellationToken);

                if (parties.Count == 0)
                    break;

                foreach (var party in parties)
                {
                    var partyId = party.Id ?? "";

                    if (!customersBySourceId.TryGetValue(partyId, out var customer))
                    {
                        report.Discrepancies.Add(new ReconciliationDiscrepancy
                        {
                            RecordId = partyId,
                            DiscrepancyType = DiscrepancyTypes.Missing,
                            SourceValue = party.PartyId,
                            TargetValue = "Not Found"
                        });
                        continue;
                    }

                    seenSourceIds.Add(partyId);

                    CheckFieldMismatch(report, partyId, "Name", party.Name, customer.Name);
                    CheckFieldMismatch(report, partyId, "Email", party.Email, customer.Email);
                    CheckFieldMismatch(report, partyId, "Phone", party.Phone, customer.Phone);
                }

                skip += PageSize;
            }

            // Target -> source: customers whose source record no longer exists in the range
            foreach (var (sourceId, customer) in customersBySourceId)
            {
                if (seenSourceIds.Contains(sourceId))
                    continue;

                report.Discrepancies.Add(new ReconciliationDiscrepancy
                {
                    RecordId = sourceId,
                    DiscrepancyType = DiscrepancyTypes.Extra,
                    SourceValue = "Not Found",
                    TargetValue = customer.CustomerId
                });
            }

            report.MismatchCount = report.Discrepancies.Count;
            report.AffectedRecordCount = report.Discrepancies
                .Select(d => d.RecordId)
                .Distinct()
                .Count();

            // Accuracy = share of distinct records (source records + extra target records) with no discrepancy
            var extraCount = report.Discrepancies.Count(d => d.DiscrepancyType == DiscrepancyTypes.Extra);
            var totalRecords = report.SourceRecordCount + extraCount;
            report.DataAccuracyPercentage = totalRecords > 0
                ? Math.Max(0, (totalRecords - report.AffectedRecordCount) / (double)totalRecords * 100)
                : 100;

            report.IsReconciled = report.SourceRecordCount == report.TargetRecordCount
                                  && report.MismatchCount == 0;

            report.Summary = $"Reconciliation completed. " +
                           $"Source: {report.SourceRecordCount}, Target: {report.TargetRecordCount}, " +
                           $"Discrepancies: {report.MismatchCount} across {report.AffectedRecordCount} records, " +
                           $"Accuracy: {report.DataAccuracyPercentage:F2}%";

            LogInfo(report.Summary);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            report.ErrorMessage = ex.Message;
            report.Summary = $"Reconciliation failed: {ex.Message}";
            LogError("Reconciliation failed", ex);
        }

        return report;
    }

    private static void CheckFieldMismatch(
        ReconciliationReport report,
        string recordId,
        string fieldName,
        string? sourceValue,
        string? targetValue)
    {
        var source = sourceValue ?? "";
        var target = targetValue ?? "";

        if (!string.Equals(source, target, StringComparison.Ordinal))
        {
            report.Discrepancies.Add(new ReconciliationDiscrepancy
            {
                RecordId = recordId,
                FieldName = fieldName,
                SourceValue = source,
                TargetValue = target,
                DiscrepancyType = DiscrepancyTypes.Mismatch
            });
        }
    }
}
