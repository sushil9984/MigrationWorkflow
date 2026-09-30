using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using MigrationWorkflow.Domain.Interfaces;
using MigrationWorkflow.Domain.Models;

namespace MigrationWorkflow.Infrastructure.Services;

/// <summary>
/// Uses Semantic Kernel with OpenAI or Azure OpenAI to enrich reconciliation analysis.
/// </summary>
public class SemanticKernelAnalysisNarrativeService : IAnalysisNarrativeService
{
    private readonly ILogger<SemanticKernelAnalysisNarrativeService> _logger;
    private readonly Kernel? _kernel;
    private readonly string _provider = string.Empty;
    private readonly string _model = string.Empty;

    public bool IsEnabled => _kernel != null;
    public string Provider => _provider;
    public string Model => _model;

    public SemanticKernelAnalysisNarrativeService(
        IConfiguration configuration,
        ILogger<SemanticKernelAnalysisNarrativeService> logger)
    {
        _logger = logger;

        var azureEndpoint = configuration["AzureOpenAI:Endpoint"];
        var azureApiKey = configuration["AzureOpenAI:ApiKey"];
        var azureDeployment = configuration["AzureOpenAI:DeploymentName"];
        var openAiApiKey = configuration["OpenAI:ApiKey"];
        var openAiModel = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        if (!string.IsNullOrWhiteSpace(azureEndpoint)
            && !string.IsNullOrWhiteSpace(azureApiKey)
            && !string.IsNullOrWhiteSpace(azureDeployment))
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddAzureOpenAIChatCompletion(
                azureDeployment,
                azureEndpoint,
                azureApiKey);

            _kernel = kernelBuilder.Build();
            _provider = "AzureOpenAI";
            _model = azureDeployment;
            _logger.LogInformation("Semantic Kernel analysis narrative service configured for Azure OpenAI");
            return;
        }

        if (!string.IsNullOrWhiteSpace(openAiApiKey))
        {
            var kernelBuilder = Kernel.CreateBuilder();
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: openAiModel,
                apiKey: openAiApiKey);

            _kernel = kernelBuilder.Build();
            _provider = "OpenAI";
            _model = openAiModel;
            _logger.LogInformation("Semantic Kernel analysis narrative service configured for OpenAI");
            return;
        }

        _logger.LogWarning("Semantic Kernel analysis narrative service is disabled because no OpenAI or Azure OpenAI configuration was found");
    }

    public async Task<AnalysisNarrative?> GenerateNarrativeAsync(
        AnalysisRequest request,
        AnalysisResult currentAnalysis,
        CancellationToken cancellationToken = default)
    {
        var debugResponse = await GenerateDebugResponseAsync(request, currentAnalysis, cancellationToken);
        return debugResponse.Narrative;
    }

    public async Task<AnalysisDebugResponse> GenerateDebugResponseAsync(
        AnalysisRequest request,
        AnalysisResult currentAnalysis,
        CancellationToken cancellationToken = default)
    {
        var prompt = BuildPrompt(request, currentAnalysis);

        if (_kernel == null)
        {
            return new AnalysisDebugResponse
            {
                IsEnabled = false,
                Provider = _provider,
                Model = _model,
                Prompt = prompt,
                ErrorMessage = "No OpenAI or Azure OpenAI configuration is active."
            };
        }

        _logger.LogDebug("LLM analysis prompt: {Prompt}", prompt);

        var rawResponse = string.Empty;
        var cleanedJson = string.Empty;

        try
        {
            var executionSettings = new OpenAIPromptExecutionSettings
            {
                ResponseFormat = "json_object",
                Temperature = 0,
                MaxTokens = 1500
            };

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(60));

            var response = await _kernel.InvokePromptAsync(
                prompt,
                new KernelArguments(executionSettings),
                cancellationToken: timeoutCts.Token);
            rawResponse = response.ToString();
            cleanedJson = CleanupJson(rawResponse);

            _logger.LogDebug("LLM raw narrative response: {RawResponse}", rawResponse);
            _logger.LogDebug("LLM cleaned narrative JSON: {CleanedJson}", cleanedJson);

            var narrativeResponse = JsonSerializer.Deserialize<LlmNarrativeResponse>(cleanedJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return new AnalysisDebugResponse
            {
                IsEnabled = true,
                Provider = _provider,
                Model = _model,
                Prompt = prompt,
                RawResponse = rawResponse,
                CleanedJson = cleanedJson,
                Narrative = narrativeResponse == null
                    ? null
                    : new AnalysisNarrative
                    {
                        ExecutiveSummary = narrativeResponse.ExecutiveSummary ?? string.Empty,
                        DetailedAnalysis = narrativeResponse.DetailedAnalysis ?? string.Empty,
                        KeyFindings = narrativeResponse.KeyFindings ?? new List<string>(),
                        Recommendations = narrativeResponse.Recommendations ?? new List<string>(),
                        CriticalIssues = narrativeResponse.CriticalIssues ?? new List<string>(),
                        Provider = _provider,
                        Model = _model
                    },
                ErrorMessage = narrativeResponse == null ? "The LLM response could not be parsed into the expected JSON schema." : null
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "LLM narrative debug request failed");

            return new AnalysisDebugResponse
            {
                IsEnabled = true,
                Provider = _provider,
                Model = _model,
                Prompt = prompt,
                RawResponse = rawResponse,
                CleanedJson = cleanedJson,
                ErrorMessage = ex.Message
            };
        }
    }

    private static string BuildPrompt(AnalysisRequest request, AnalysisResult currentAnalysis)
    {
        var discrepancySample = request.ReconciliationReport.Discrepancies
            .Take(25)
            .Select(d => new
            {
                // Actual values (emails, phones, names) are customer PII and are deliberately
                // not sent to the external model; only the shape of the discrepancy is.
                d.RecordId,
                d.FieldName,
                d.DiscrepancyType,
                SourceValuePresent = !string.IsNullOrEmpty(d.SourceValue),
                TargetValuePresent = !string.IsNullOrEmpty(d.TargetValue)
            })
            .ToList();

        var promptPayload = new
        {
            CurrentAssessment = new
            {
                currentAnalysis.OverallStatus,
                currentAnalysis.QualityScore,
                currentAnalysis.DiscrepancyTypeBreakdown,
                currentAnalysis.KeyFindings,
                currentAnalysis.Recommendations,
                currentAnalysis.CriticalIssues
            },
            ReconciliationReport = new
            {
                request.ReconciliationReport.ReportId,
                request.ReconciliationReport.MigrationId,
                request.ReconciliationReport.GeneratedAt,
                request.ReconciliationReport.SourceRecordCount,
                request.ReconciliationReport.TargetRecordCount,
                request.ReconciliationReport.MismatchCount,
                request.ReconciliationReport.DataAccuracyPercentage,
                request.ReconciliationReport.IsReconciled,
                request.ReconciliationReport.Summary,
                TotalDiscrepancies = request.ReconciliationReport.Discrepancies.Count,
                DiscrepancySample = discrepancySample
            }
        };

        var payloadJson = JsonSerializer.Serialize(promptPayload, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var prompt = new StringBuilder();
        prompt.AppendLine("You are a migration quality analyst.");
        prompt.AppendLine("Review the data migration reconciliation payload and enrich the existing deterministic assessment.");
        prompt.AppendLine("Return valid JSON only. Do not wrap the response in markdown or code fences.");
        prompt.AppendLine("Keep all statements grounded in the supplied data.");
        prompt.AppendLine();
        prompt.AppendLine("Required JSON schema:");
        prompt.AppendLine("{");
        prompt.AppendLine("  \"executiveSummary\": \"string\",");
        prompt.AppendLine("  \"detailedAnalysis\": \"string\",");
        prompt.AppendLine("  \"keyFindings\": [\"string\"],");
        prompt.AppendLine("  \"recommendations\": [\"string\"],");
        prompt.AppendLine("  \"criticalIssues\": [\"string\"]");
        prompt.AppendLine("}");
        prompt.AppendLine();
        prompt.AppendLine("Rules:");
        prompt.AppendLine("- Provide 3 to 5 concise key findings.");
        prompt.AppendLine("- Provide 3 to 5 actionable recommendations.");
        prompt.AppendLine("- Only include critical issues when the data supports them.");
        prompt.AppendLine("- Keep the executive summary to 2 sentences max.");
        prompt.AppendLine("- Mention data accuracy, reconciliation status, and the most important discrepancy pattern.");
        prompt.AppendLine();
        prompt.AppendLine("Payload:");
        prompt.Append(payloadJson);

        return prompt.ToString();
    }

    private static string CleanupJson(string rawResponse)
    {
        var cleaned = rawResponse.Trim();

        if (cleaned.StartsWith("```json", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[7..].Trim();
        }
        else if (cleaned.StartsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[3..].Trim();
        }

        if (cleaned.EndsWith("```", StringComparison.OrdinalIgnoreCase))
        {
            cleaned = cleaned[..^3].Trim();
        }

        return cleaned;
    }

    private sealed class LlmNarrativeResponse
    {
        public string? ExecutiveSummary { get; set; }
        public string? DetailedAnalysis { get; set; }
        public List<string>? KeyFindings { get; set; }
        public List<string>? Recommendations { get; set; }
        public List<string>? CriticalIssues { get; set; }
    }
}