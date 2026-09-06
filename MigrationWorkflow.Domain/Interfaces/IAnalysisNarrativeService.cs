using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Generates an optional natural-language analysis narrative for a reconciliation report.
/// </summary>
public interface IAnalysisNarrativeService
{
    bool IsEnabled { get; }
    string Provider { get; }
    string Model { get; }

    Task<AnalysisNarrative?> GenerateNarrativeAsync(
        AnalysisRequest request,
        AnalysisResult currentAnalysis,
        CancellationToken cancellationToken = default);

    Task<AnalysisDebugResponse> GenerateDebugResponseAsync(
        AnalysisRequest request,
        AnalysisResult currentAnalysis,
        CancellationToken cancellationToken = default);
}