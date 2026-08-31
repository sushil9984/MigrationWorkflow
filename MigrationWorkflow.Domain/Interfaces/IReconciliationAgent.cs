using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Agent responsible for generating reconciliation reports after migration
/// </summary>
public interface IReconciliationAgent : IAgent<ReconciliationRequest, ReconciliationReport>
{
}
