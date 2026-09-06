namespace MigrationWorkflow.Domain.Models;

/// <summary>
/// Result from analysis agent
/// </summary>
public class AnalysisResult
{
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();
    public string ReportId { get; set; } = string.Empty;
    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    public string ExecutiveSummary { get; set; } = string.Empty;
    
    // Overall assessment
    public string OverallStatus { get; set; } = string.Empty; // Success, Warning, Failed
    public double QualityScore { get; set; } // 0-100
    
    // Key findings
    public List<string> KeyFindings { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();
    public List<string> CriticalIssues { get; set; } = new();
    
    // Statistics
    public Dictionary<string, int> DiscrepancyTypeBreakdown { get; set; } = new();
    
    // Detailed analysis
    public string DetailedAnalysis { get; set; } = string.Empty;
    public bool LlmEnhanced { get; set; }
    public string? LlmProvider { get; set; }
    public string? LlmModel { get; set; }
}
