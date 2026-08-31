using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Agent responsible for analyzing reconciliation reports
/// </summary>
public interface IAnalysisAgent : IAgent<AnalysisRequest, AnalysisResult>
{
}
