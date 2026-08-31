namespace MigrationWorkflow.Domain.Interfaces;

/// <summary>
/// Base interface for all agents in the workflow
/// </summary>
/// <typeparam name="TInput">Input type for the agent</typeparam>
/// <typeparam name="TOutput">Output type from the agent</typeparam>
public interface IAgent<TInput, TOutput>
{
    /// <summary>
    /// Executes the agent's task asynchronously
    /// </summary>
    /// <param name="input">Input data for the agent</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Result of the agent's execution</returns>
    Task<TOutput> ExecuteAsync(TInput input, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unique name/identifier for the agent
    /// </summary>
    string AgentName { get; }
}
