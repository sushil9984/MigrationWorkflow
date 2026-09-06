using Microsoft.Extensions.Logging;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Application.Agents;

/// <summary>
/// Agent responsible for analyzing reconciliation reports
/// </summary>
public class AnalysisAgent : AgentBase<AnalysisRequest, AnalysisResult>, IAnalysisAgent
{
    private readonly IAnalysisNarrativeService _analysisNarrativeService;

    public override string AgentName => "AnalysisAgent";
    
    public AnalysisAgent(
        IAnalysisNarrativeService analysisNarrativeService,
        ILogger<AnalysisAgent> logger) : base(logger)
    {
        _analysisNarrativeService = analysisNarrativeService;
    }
    
    public override async Task<AnalysisResult> ExecuteAsync(
        AnalysisRequest input,
        CancellationToken cancellationToken = default)
    {
        LogInfo("Starting reconciliation report analysis");
        
        var report = input.ReconciliationReport;
        var result = new AnalysisResult
        {
            ReportId = report.ReportId,
            AnalyzedAt = DateTime.UtcNow
        };
        
        try
        {
            // Calculate quality score
            result.QualityScore = report.DataAccuracyPercentage;
            
            // Determine overall status
            result.OverallStatus = result.QualityScore switch
            {
                >= 99.0 => "Success",
                >= 95.0 => "Warning",
                _ => "Failed"
            };
            
            // Analyze discrepancy types
            result.DiscrepancyTypeBreakdown = report.Discrepancies
                .GroupBy(d => d.DiscrepancyType)
                .ToDictionary(g => g.Key, g => g.Count());
            
            // Generate key findings
            GenerateKeyFindings(result, report);
            
            // Generate recommendations
            GenerateRecommendations(result, report);
            
            // Identify critical issues
            IdentifyCriticalIssues(result, report);
            
            // Generate detailed analysis if requested
            if (input.GenerateDetailedAnalysis)
            {
                GenerateDetailedAnalysis(result, report);
            }

            await EnrichWithLlmNarrativeAsync(input, result, cancellationToken);
            
            LogInfo($"Analysis completed. Status: {result.OverallStatus}, Quality Score: {result.QualityScore:F2}%");
        }
        catch (Exception ex)
        {
            LogError("Analysis failed", ex);
            result.CriticalIssues.Add($"Analysis failed: {ex.Message}");
        }
        
        return await Task.FromResult(result);
    }
    
    private void GenerateKeyFindings(AnalysisResult result, ReconciliationReport report)
    {
        result.KeyFindings.Add($"Total records processed: {report.TargetRecordCount}");
        result.KeyFindings.Add($"Data accuracy: {report.DataAccuracyPercentage:F2}%");
        result.KeyFindings.Add($"Migration is {(report.IsReconciled ? "fully" : "not fully")} reconciled");
        
        if (report.SourceRecordCount != report.TargetRecordCount)
        {
            var diff = report.SourceRecordCount - report.TargetRecordCount;
            result.KeyFindings.Add($"Count mismatch detected: {Math.Abs(diff)} records difference");
        }
        
        if (report.Discrepancies.Any())
        {
            result.KeyFindings.Add($"Found {report.Discrepancies.Count} data discrepancies requiring attention");
        }
    }
    
    private void GenerateRecommendations(AnalysisResult result, ReconciliationReport report)
    {
        if (report.DataAccuracyPercentage < 100)
        {
            result.Recommendations.Add("Review and correct data discrepancies before proceeding");
        }
        
        if (report.SourceRecordCount != report.TargetRecordCount)
        {
            result.Recommendations.Add("Investigate missing or extra records in target database");
        }
        
        var missingRecords = report.Discrepancies.Count(d => d.DiscrepancyType == "Missing");
        if (missingRecords > 0)
        {
            result.Recommendations.Add($"Re-run migration for {missingRecords} missing records");
        }
        
        var mismatchRecords = report.Discrepancies.Count(d => d.DiscrepancyType == "Mismatch");
        if (mismatchRecords > 0)
        {
            result.Recommendations.Add($"Validate transformation logic for {mismatchRecords} mismatched records");
        }
        
        if (report.IsReconciled)
        {
            result.Recommendations.Add("Migration completed successfully. Safe to proceed with next steps");
        }
    }
    
    private void IdentifyCriticalIssues(AnalysisResult result, ReconciliationReport report)
    {
        if (report.DataAccuracyPercentage < 90)
        {
            result.CriticalIssues.Add($"CRITICAL: Low data accuracy ({report.DataAccuracyPercentage:F2}%)");
        }
        
        var countDiff = Math.Abs(report.SourceRecordCount - report.TargetRecordCount);
        if (countDiff > report.SourceRecordCount * 0.05) // More than 5% difference
        {
            result.CriticalIssues.Add($"CRITICAL: Significant record count mismatch ({countDiff} records)");
        }
        
        var missingCount = report.Discrepancies.Count(d => d.DiscrepancyType == "Missing");
        if (missingCount > 10)
        {
            result.CriticalIssues.Add($"CRITICAL: High number of missing records ({missingCount})");
        }
    }
    
    private void GenerateDetailedAnalysis(AnalysisResult result, ReconciliationReport report)
    {
        var analysis = new System.Text.StringBuilder();
        
        analysis.AppendLine("=== DETAILED MIGRATION ANALYSIS ===");
        analysis.AppendLine();
        analysis.AppendLine($"Report ID: {report.ReportId}");
        analysis.AppendLine($"Migration ID: {report.MigrationId}");
        analysis.AppendLine($"Generated At: {report.GeneratedAt:yyyy-MM-dd HH:mm:ss UTC}");
        analysis.AppendLine();
        
        analysis.AppendLine("--- RECORD COUNTS ---");
        analysis.AppendLine($"Source (MongoDB): {report.SourceRecordCount}");
        analysis.AppendLine($"Target (PostgreSQL): {report.TargetRecordCount}");
        analysis.AppendLine($"Difference: {report.SourceRecordCount - report.TargetRecordCount}");
        analysis.AppendLine();
        
        analysis.AppendLine("--- DATA QUALITY ---");
        analysis.AppendLine($"Accuracy: {report.DataAccuracyPercentage:F2}%");
        analysis.AppendLine($"Discrepancies: {report.MismatchCount}");
        analysis.AppendLine($"Is Reconciled: {report.IsReconciled}");
        analysis.AppendLine();
        
        if (result.DiscrepancyTypeBreakdown.Any())
        {
            analysis.AppendLine("--- DISCREPANCY BREAKDOWN ---");
            foreach (var kvp in result.DiscrepancyTypeBreakdown)
            {
                analysis.AppendLine($"{kvp.Key}: {kvp.Value}");
            }
            analysis.AppendLine();
        }
        
        if (result.CriticalIssues.Any())
        {
            analysis.AppendLine("--- CRITICAL ISSUES ---");
            foreach (var issue in result.CriticalIssues)
            {
                analysis.AppendLine($"• {issue}");
            }
            analysis.AppendLine();
        }
        
        analysis.AppendLine("--- RECOMMENDATIONS ---");
        foreach (var recommendation in result.Recommendations)
        {
            analysis.AppendLine($"• {recommendation}");
        }
        
        result.DetailedAnalysis = analysis.ToString();
    }

    private async Task EnrichWithLlmNarrativeAsync(
        AnalysisRequest input,
        AnalysisResult result,
        CancellationToken cancellationToken)
    {
        try
        {
            var narrative = await _analysisNarrativeService.GenerateNarrativeAsync(input, result, cancellationToken);
            if (narrative == null)
            {
                return;
            }

            result.ExecutiveSummary = narrative.ExecutiveSummary;

            if (!string.IsNullOrWhiteSpace(narrative.DetailedAnalysis))
            {
                result.DetailedAnalysis = narrative.DetailedAnalysis;
            }

            result.KeyFindings = MergeUnique(result.KeyFindings, narrative.KeyFindings);
            result.Recommendations = MergeUnique(result.Recommendations, narrative.Recommendations);
            result.CriticalIssues = MergeUnique(result.CriticalIssues, narrative.CriticalIssues);
            result.LlmEnhanced = true;
            result.LlmProvider = narrative.Provider;
            result.LlmModel = narrative.Model;

            LogInfo($"LLM narrative added using {narrative.Provider}/{narrative.Model}");
        }
        catch (Exception ex)
        {
            LogWarning($"LLM narrative enhancement skipped: {ex.Message}");
        }
    }

    private static List<string> MergeUnique(IEnumerable<string> current, IEnumerable<string> incoming)
    {
        var merged = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var item in current.Concat(incoming))
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            var trimmed = item.Trim();
            if (seen.Add(trimmed))
            {
                merged.Add(trimmed);
            }
        }

        return merged;
    }
}
