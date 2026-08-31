using Microsoft.Extensions.Logging;
using MigrationWorkflow.Domain.Interfaces;

namespace MigrationWorkflow.Application.Agents;

/// <summary>
/// Base class for all agents providing common functionality
/// </summary>
public abstract class AgentBase<TInput, TOutput> : IAgent<TInput, TOutput>
{
    protected readonly ILogger _logger;
    
    protected AgentBase(ILogger logger)
    {
        _logger = logger;
    }
    
    public abstract string AgentName { get; }
    
    public abstract Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    
    protected void LogInfo(string message)
    {
        _logger.LogInformation($"[{AgentName}] {message}");
    }
    
    protected void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
            _logger.LogError(ex, $"[{AgentName}] {message}");
        else
            _logger.LogError($"[{AgentName}] {message}");
    }
    
    protected void LogWarning(string message)
    {
        _logger.LogWarning($"[{AgentName}] {message}");
    }
}
